using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DevHub.Api.IntegrationTests.Harness;
using DevHub.Application.Auth.Contracts;
using DevHub.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace DevHub.Api.IntegrationTests.Workspaces;

/// <summary>
/// DEVHUB-025: <c>/api/workspaces/{id}/members</c> (api-endpoints.md §2). Scoping follows the
/// same pattern as <see cref="WorkspaceEndpointTests"/> — a stranger gets 404 — plus the
/// membership-specific rules: any member may read the list, only an owner may mutate someone
/// else, and the last owner can never be demoted or removed by any path.
/// </summary>
public sealed class MemberEndpointTests(DevHubApiFactory api) : IClassFixture<DevHubApiFactory>
{
    private const string Errors = "https://devhub.dev/errors/";

    private readonly HttpClient _client = api.CreateClient();

    // ---- List -------------------------------------------------------------------------------

    [Fact]
    public async Task Any_member_can_list_members_with_user_details_and_role()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync("member");
        var workspace = await CreateOkAsync(owner);
        await SeedMemberAsync(Id(workspace), member.User.Id, WorkspaceRole.Member);

        var response = await SendAsync(HttpMethod.Get, $"/api/workspaces/{Id(workspace)}/members", member);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Collection(
            body.EnumerateArray(),
            first =>
            {
                Assert.Equal("Owner", first.GetProperty("role").GetString());
                Assert.Equal(owner.User.Email, first.GetProperty("user").GetProperty("email").GetString());
            },
            second =>
            {
                Assert.Equal("Member", second.GetProperty("role").GetString());
                Assert.Equal(member.User.Email, second.GetProperty("user").GetProperty("email").GetString());
                Assert.Equal(member.User.Id, second.GetProperty("user").GetProperty("id").GetGuid());
            });
    }

    [Fact]
    public async Task A_non_member_gets_404_for_list_identical_to_a_workspace_that_does_not_exist()
    {
        var owner = await RegisterAsync();
        var stranger = await RegisterAsync("stranger");
        var workspace = await CreateOkAsync(owner);

        var existing = await SendAsync(HttpMethod.Get, $"/api/workspaces/{Id(workspace)}/members", stranger);
        var missing = await SendAsync(HttpMethod.Get, $"/api/workspaces/{Guid.CreateVersion7()}/members", stranger);

        await AssertProblemAsync(existing, HttpStatusCode.NotFound, "workspaces.not_found");
        await AssertProblemAsync(missing, HttpStatusCode.NotFound, "workspaces.not_found");
    }

    // ---- Add ----------------------------------------------------------------------------------

    [Fact]
    public async Task An_owner_can_add_an_existing_user_by_email()
    {
        var owner = await RegisterAsync();
        var invitee = await RegisterAsync("invitee");
        var workspace = await CreateOkAsync(owner);

        var response = await SendAsync(
            HttpMethod.Post, $"/api/workspaces/{Id(workspace)}/members", owner, new { email = invitee.User.Email, role = "Member" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Member", body.GetProperty("role").GetString());
        Assert.Equal(invitee.User.Id, body.GetProperty("user").GetProperty("id").GetGuid());
        Assert.Equal($"/api/workspaces/{Id(workspace)}/members/{body.GetProperty("id").GetGuid()}", response.Headers.Location?.OriginalString);

        var saved = await LoadAsync(Id(workspace));
        Assert.Contains(saved.Members, candidate => candidate.UserId == invitee.User.Id && candidate.Role == WorkspaceRole.Member);
    }

    [Fact]
    public async Task Adding_an_unknown_email_returns_404_with_a_humane_message()
    {
        var owner = await RegisterAsync();
        var workspace = await CreateOkAsync(owner);

        var response = await SendAsync(
            HttpMethod.Post, $"/api/workspaces/{Id(workspace)}/members", owner, new { email = AuthApi.UniqueEmail("ghost"), role = "Member" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(Errors + "workspaces.user_not_found", problem.GetProperty("type").GetString());
        Assert.Equal("No DevHub account with that email.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Adding_an_existing_member_again_returns_409()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync("member");
        var workspace = await CreateOkAsync(owner);
        await SeedMemberAsync(Id(workspace), member.User.Id, WorkspaceRole.Member);

        var response = await SendAsync(
            HttpMethod.Post, $"/api/workspaces/{Id(workspace)}/members", owner, new { email = member.User.Email, role = "Owner" });

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "workspaces.already_member");
    }

    [Fact]
    public async Task A_member_who_is_not_an_owner_gets_403_on_add_and_nothing_changes()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync("member");
        var invitee = await RegisterAsync("invitee");
        var workspace = await CreateOkAsync(owner);
        await SeedMemberAsync(Id(workspace), member.User.Id, WorkspaceRole.Member);

        var response = await SendAsync(
            HttpMethod.Post, $"/api/workspaces/{Id(workspace)}/members", member, new { email = invitee.User.Email, role = "Member" });

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "workspaces.owner_required");
        Assert.Equal(2, (await LoadAsync(Id(workspace))).Members.Count);
    }

    [Fact]
    public async Task A_non_member_gets_404_on_add_never_403()
    {
        var owner = await RegisterAsync();
        var stranger = await RegisterAsync("stranger");
        var invitee = await RegisterAsync("invitee");
        var workspace = await CreateOkAsync(owner);

        var response = await SendAsync(
            HttpMethod.Post, $"/api/workspaces/{Id(workspace)}/members", stranger, new { email = invitee.User.Email, role = "Member" });

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "workspaces.not_found");
    }

    [Theory]
    [InlineData(null, "Member")]
    [InlineData("", "Member")]
    [InlineData("not-an-email", "Member")]
    public async Task Add_with_an_invalid_email_returns_400_on_email(string? email, string role)
    {
        var owner = await RegisterAsync();
        var workspace = await CreateOkAsync(owner);

        var response = await SendAsync(HttpMethod.Post, $"/api/workspaces/{Id(workspace)}/members", owner, new { email, role });

        await AssertValidationErrorAsync(response, "email");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Admin")]
    public async Task Add_with_an_invalid_role_returns_400_on_role(string? role)
    {
        var owner = await RegisterAsync();
        var invitee = await RegisterAsync("invitee");
        var workspace = await CreateOkAsync(owner);

        var response = await SendAsync(
            HttpMethod.Post, $"/api/workspaces/{Id(workspace)}/members", owner, new { email = invitee.User.Email, role });

        await AssertValidationErrorAsync(response, "role");
    }

    // ---- Change role ----------------------------------------------------------------------

    [Fact]
    public async Task An_owner_can_promote_and_demote_a_member()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync("member");
        var workspace = await CreateOkAsync(owner);
        var memberId = await SeedMemberAsync(Id(workspace), member.User.Id, WorkspaceRole.Member);

        var promoted = await SendAsync(
            HttpMethod.Patch, $"/api/workspaces/{Id(workspace)}/members/{memberId}", owner, new { role = "Owner" });

        Assert.Equal(HttpStatusCode.OK, promoted.StatusCode);
        Assert.Equal("Owner", (await promoted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString());

        var demoted = await SendAsync(
            HttpMethod.Patch, $"/api/workspaces/{Id(workspace)}/members/{memberId}", owner, new { role = "Member" });

        Assert.Equal(HttpStatusCode.OK, demoted.StatusCode);
        Assert.Equal("Member", (await demoted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString());
    }

    [Fact]
    public async Task Demoting_the_last_owner_returns_422_and_nothing_changes()
    {
        var owner = await RegisterAsync();
        var workspace = await CreateOkAsync(owner);
        var ownerMemberId = (await LoadAsync(Id(workspace))).Members.Single().Id;

        var response = await SendAsync(
            HttpMethod.Patch, $"/api/workspaces/{Id(workspace)}/members/{ownerMemberId}", owner, new { role = "Member" });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "workspaces.last_owner");
        Assert.Equal(WorkspaceRole.Owner, (await LoadAsync(Id(workspace))).Members.Single().Role);
    }

    [Fact]
    public async Task Demoting_an_owner_while_a_second_owner_remains_is_allowed()
    {
        var owner = await RegisterAsync();
        var secondOwner = await RegisterAsync("owner2");
        var workspace = await CreateOkAsync(owner);
        var secondOwnerId = await SeedMemberAsync(Id(workspace), secondOwner.User.Id, WorkspaceRole.Owner);

        var response = await SendAsync(
            HttpMethod.Patch, $"/api/workspaces/{Id(workspace)}/members/{secondOwnerId}", owner, new { role = "Member" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_member_who_is_not_an_owner_gets_403_on_role_change()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync("member");
        var other = await RegisterAsync("other");
        var workspace = await CreateOkAsync(owner);
        await SeedMemberAsync(Id(workspace), member.User.Id, WorkspaceRole.Member);
        var otherId = await SeedMemberAsync(Id(workspace), other.User.Id, WorkspaceRole.Member);

        var response = await SendAsync(
            HttpMethod.Patch, $"/api/workspaces/{Id(workspace)}/members/{otherId}", member, new { role = "Owner" });

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "workspaces.owner_required");
    }

    [Fact]
    public async Task A_non_member_gets_404_on_role_change_never_403()
    {
        var owner = await RegisterAsync();
        var stranger = await RegisterAsync("stranger");
        var workspace = await CreateOkAsync(owner);
        var ownerMemberId = (await LoadAsync(Id(workspace))).Members.Single().Id;

        var response = await SendAsync(
            HttpMethod.Patch, $"/api/workspaces/{Id(workspace)}/members/{ownerMemberId}", stranger, new { role = "Member" });

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "workspaces.not_found");
    }

    [Fact]
    public async Task Changing_the_role_of_an_unknown_member_returns_404()
    {
        var owner = await RegisterAsync();
        var workspace = await CreateOkAsync(owner);

        var response = await SendAsync(
            HttpMethod.Patch, $"/api/workspaces/{Id(workspace)}/members/{Guid.CreateVersion7()}", owner, new { role = "Member" });

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "workspaces.member_not_found");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Admin")]
    public async Task Change_role_with_an_invalid_role_returns_400(string? role)
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync("member");
        var workspace = await CreateOkAsync(owner);
        var memberId = await SeedMemberAsync(Id(workspace), member.User.Id, WorkspaceRole.Member);

        var response = await SendAsync(HttpMethod.Patch, $"/api/workspaces/{Id(workspace)}/members/{memberId}", owner, new { role });

        await AssertValidationErrorAsync(response, "role");
    }

    // ---- Remove / leave ---------------------------------------------------------------------

    [Fact]
    public async Task An_owner_can_remove_a_member()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync("member");
        var workspace = await CreateOkAsync(owner);
        var memberId = await SeedMemberAsync(Id(workspace), member.User.Id, WorkspaceRole.Member);

        var response = await SendAsync(HttpMethod.Delete, $"/api/workspaces/{Id(workspace)}/members/{memberId}", owner);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.DoesNotContain((await LoadAsync(Id(workspace))).Members, candidate => candidate.Id == memberId);
    }

    [Fact]
    public async Task A_member_can_remove_themselves_to_leave_without_being_an_owner()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync("member");
        var workspace = await CreateOkAsync(owner);
        var memberId = await SeedMemberAsync(Id(workspace), member.User.Id, WorkspaceRole.Member);

        var response = await SendAsync(HttpMethod.Delete, $"/api/workspaces/{Id(workspace)}/members/{memberId}", member);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.DoesNotContain((await LoadAsync(Id(workspace))).Members, candidate => candidate.Id == memberId);
    }

    [Fact]
    public async Task A_member_who_is_not_an_owner_cannot_remove_someone_else()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync("member");
        var other = await RegisterAsync("other");
        var workspace = await CreateOkAsync(owner);
        await SeedMemberAsync(Id(workspace), member.User.Id, WorkspaceRole.Member);
        var otherId = await SeedMemberAsync(Id(workspace), other.User.Id, WorkspaceRole.Member);

        var response = await SendAsync(HttpMethod.Delete, $"/api/workspaces/{Id(workspace)}/members/{otherId}", member);

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "workspaces.owner_required");
        Assert.Contains((await LoadAsync(Id(workspace))).Members, candidate => candidate.Id == otherId);
    }

    [Fact]
    public async Task Removing_the_last_owner_returns_422_even_as_a_self_leave()
    {
        var owner = await RegisterAsync();
        var workspace = await CreateOkAsync(owner);
        var ownerMemberId = (await LoadAsync(Id(workspace))).Members.Single().Id;

        var response = await SendAsync(HttpMethod.Delete, $"/api/workspaces/{Id(workspace)}/members/{ownerMemberId}", owner);

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "workspaces.last_owner");
        Assert.Single((await LoadAsync(Id(workspace))).Members);
    }

    [Fact]
    public async Task Removing_an_owner_while_a_second_owner_remains_is_allowed()
    {
        var owner = await RegisterAsync();
        var secondOwner = await RegisterAsync("owner2");
        var workspace = await CreateOkAsync(owner);
        var secondOwnerId = await SeedMemberAsync(Id(workspace), secondOwner.User.Id, WorkspaceRole.Owner);

        var response = await SendAsync(HttpMethod.Delete, $"/api/workspaces/{Id(workspace)}/members/{secondOwnerId}", owner);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task A_non_member_gets_404_on_remove_never_403()
    {
        var owner = await RegisterAsync();
        var stranger = await RegisterAsync("stranger");
        var workspace = await CreateOkAsync(owner);
        var ownerMemberId = (await LoadAsync(Id(workspace))).Members.Single().Id;

        var response = await SendAsync(HttpMethod.Delete, $"/api/workspaces/{Id(workspace)}/members/{ownerMemberId}", stranger);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "workspaces.not_found");
    }

    [Fact]
    public async Task Removing_an_unknown_member_returns_404()
    {
        var owner = await RegisterAsync();
        var workspace = await CreateOkAsync(owner);

        var response = await SendAsync(HttpMethod.Delete, $"/api/workspaces/{Id(workspace)}/members/{Guid.CreateVersion7()}", owner);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "workspaces.member_not_found");
    }

    // ---- Helpers --------------------------------------------------------------------------

    private static Guid Id(JsonElement workspace) => workspace.GetProperty("id").GetGuid();

    private Task<AuthResponse> RegisterAsync(string prefix = "owner") => _client.RegisterOkAsync(AuthApi.UniqueEmail(prefix));

    private async Task<JsonElement> CreateOkAsync(AuthResponse caller)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/workspaces", caller, new { name = $"Workspace {Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>
    /// Seeds a member directly through the aggregate rather than the invite endpoint, for tests
    /// that need a member in place without caring about the email-lookup step. Returns the new
    /// membership's own id, which is what the mutation endpoints under test address.
    /// </summary>
    private Task<Guid> SeedMemberAsync(Guid workspaceId, Guid userId, WorkspaceRole role) =>
        api.QueryDbAsync(async db =>
        {
            var workspace = await db.Workspaces.Include(w => w.Members).SingleAsync(w => w.Id == workspaceId);
            var member = workspace.AddMember(userId, role, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
            return member.Id;
        });

    private Task<Workspace> LoadAsync(Guid workspaceId) =>
        api.QueryDbAsync(db => db.Workspaces.AsNoTracking().Include(w => w.Members).SingleAsync(w => w.Id == workspaceId));

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, AuthResponse caller, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", caller.AccessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(Errors + code, problem.GetProperty("type").GetString());
    }

    private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(Errors + "validation", problem.GetProperty("type").GetString());
        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _), $"Expected an error on '{field}'.");
    }
}
