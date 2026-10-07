using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DevHub.Api.IntegrationTests.Harness;
using DevHub.Application.Auth.Contracts;
using DevHub.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace DevHub.Api.IntegrationTests.Projects;

/// <summary>
/// DEVHUB-032: <c>/api/projects/{projectId}/members</c> (api-endpoints.md §3). The rule under
/// test throughout: project membership narrows who can be <i>assigned</i> issues, it never
/// narrows who can <i>read</i> a project — every workspace member can see every project's member
/// list, the same as <see cref="ProjectEndpointTests.Any_workspace_member_can_get_a_project_not_just_its_creator"/>.
/// </summary>
public sealed class ProjectMemberEndpointTests(DevHubApiFactory api) : IClassFixture<DevHubApiFactory>
{
    private const string Errors = "https://devhub.dev/errors/";

    private readonly HttpClient _client = api.CreateClient();

    // ---- List -------------------------------------------------------------------------------

    [Fact]
    public async Task List_returns_the_creator_as_the_only_member_right_after_creation()
    {
        var owner = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        var project = await CreateProjectOkAsync(owner, workspaceId);

        var response = await SendAsync(HttpMethod.Get, $"/api/projects/{Id(project)}/members", owner);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await ReadMembersAsync(response);
        var member = Assert.Single(members);
        Assert.Equal(owner.User.Id, member.GetProperty("user").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task A_workspace_member_who_is_not_a_project_member_can_still_list_the_members()
    {
        var owner = await RegisterAsync();
        var outsiderOfProject = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        await SeedWorkspaceMemberAsync(workspaceId, outsiderOfProject.User.Id);
        var project = await CreateProjectOkAsync(owner, workspaceId);

        // outsiderOfProject is a workspace member but was never added to the project.
        var response = await SendAsync(HttpMethod.Get, $"/api/projects/{Id(project)}/members", outsiderOfProject);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await ReadMembersAsync(response);
        Assert.Single(members);
    }

    [Fact]
    public async Task A_non_workspace_member_gets_404_for_list_identical_to_a_project_that_does_not_exist()
    {
        var owner = await RegisterAsync();
        var stranger = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        var project = await CreateProjectOkAsync(owner, workspaceId);

        var existing = await SendAsync(HttpMethod.Get, $"/api/projects/{Id(project)}/members", stranger);
        var missing = await SendAsync(HttpMethod.Get, $"/api/projects/{Guid.CreateVersion7()}/members", stranger);

        await AssertProblemAsync(existing, HttpStatusCode.NotFound, "projects.not_found");
        await AssertProblemAsync(missing, HttpStatusCode.NotFound, "projects.not_found");
    }

    // ---- Add --------------------------------------------------------------------------------

    [Fact]
    public async Task An_owner_can_add_a_workspace_member_and_returns_201_with_a_location_header()
    {
        var owner = await RegisterAsync();
        var newMember = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        await SeedWorkspaceMemberAsync(workspaceId, newMember.User.Id);
        var project = await CreateProjectOkAsync(owner, workspaceId);

        var response = await SendAsync(
            HttpMethod.Post, $"/api/projects/{Id(project)}/members", owner, new { userId = newMember.User.Id });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            $"/api/projects/{Id(project)}/members/{body.GetProperty("id").GetGuid()}",
            response.Headers.Location?.OriginalString);
        Assert.Equal(newMember.User.Id, body.GetProperty("user").GetProperty("id").GetGuid());

        var members = await ReadMembersAsync(await SendAsync(HttpMethod.Get, $"/api/projects/{Id(project)}/members", owner));
        Assert.Equal(2, members.Count);
    }

    [Fact]
    public async Task Adding_a_user_who_is_not_a_workspace_member_returns_422_and_adds_nobody()
    {
        var owner = await RegisterAsync();
        var outsider = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        var project = await CreateProjectOkAsync(owner, workspaceId);

        // outsider never joined the workspace at all.
        var response = await SendAsync(
            HttpMethod.Post, $"/api/projects/{Id(project)}/members", owner, new { userId = outsider.User.Id });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "projects.not_workspace_member");
        Assert.Single(await ReadMembersAsync(await SendAsync(HttpMethod.Get, $"/api/projects/{Id(project)}/members", owner)));
    }

    [Fact]
    public async Task Adding_an_already_existing_member_returns_409()
    {
        var owner = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        var project = await CreateProjectOkAsync(owner, workspaceId);

        // owner is already a member from project creation.
        var response = await SendAsync(
            HttpMethod.Post, $"/api/projects/{Id(project)}/members", owner, new { userId = owner.User.Id });

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "projects.already_member");
    }

    [Fact]
    public async Task A_member_who_is_not_an_owner_gets_403_on_add_and_adds_nobody()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var newMember = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        await SeedWorkspaceMemberAsync(workspaceId, member.User.Id);
        await SeedWorkspaceMemberAsync(workspaceId, newMember.User.Id);
        var project = await CreateProjectOkAsync(owner, workspaceId);

        var response = await SendAsync(
            HttpMethod.Post, $"/api/projects/{Id(project)}/members", member, new { userId = newMember.User.Id });

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "workspaces.owner_required");
        Assert.Single(await ReadMembersAsync(await SendAsync(HttpMethod.Get, $"/api/projects/{Id(project)}/members", owner)));
    }

    // ---- Remove -----------------------------------------------------------------------------

    [Fact]
    public async Task An_owner_can_remove_a_member_and_it_disappears_from_the_list()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        await SeedWorkspaceMemberAsync(workspaceId, member.User.Id);
        var project = await CreateProjectOkAsync(owner, workspaceId);
        var memberId = await AddProjectMemberOkAsync(owner, Id(project), member.User.Id);

        var response = await SendAsync(HttpMethod.Delete, $"/api/projects/{Id(project)}/members/{memberId}", owner);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var remaining = await ReadMembersAsync(await SendAsync(HttpMethod.Get, $"/api/projects/{Id(project)}/members", owner));
        Assert.DoesNotContain(memberId, remaining.Select(m => m.GetProperty("id").GetGuid()));
    }

    [Fact]
    public async Task Removing_an_unknown_member_id_returns_404()
    {
        var owner = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        var project = await CreateProjectOkAsync(owner, workspaceId);

        var response = await SendAsync(
            HttpMethod.Delete, $"/api/projects/{Id(project)}/members/{Guid.CreateVersion7()}", owner);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "projects.member_not_found");
    }

    [Fact]
    public async Task A_member_who_is_not_an_owner_gets_403_on_remove_and_nobody_is_removed()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var another = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        await SeedWorkspaceMemberAsync(workspaceId, member.User.Id);
        await SeedWorkspaceMemberAsync(workspaceId, another.User.Id);
        var project = await CreateProjectOkAsync(owner, workspaceId);
        var anotherMemberId = await AddProjectMemberOkAsync(owner, Id(project), another.User.Id);

        var response = await SendAsync(
            HttpMethod.Delete, $"/api/projects/{Id(project)}/members/{anotherMemberId}", member);

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "workspaces.owner_required");
        var remaining = await ReadMembersAsync(await SendAsync(HttpMethod.Get, $"/api/projects/{Id(project)}/members", owner));
        Assert.Contains(anotherMemberId, remaining.Select(m => m.GetProperty("id").GetGuid()));
    }

    // ---- Helpers --------------------------------------------------------------------------

    private static Guid Id(JsonElement element) => element.GetProperty("id").GetGuid();

    private Task<AuthResponse> RegisterAsync() => _client.RegisterOkAsync(AuthApi.UniqueEmail());

    private async Task<Guid> CreateWorkspaceOkAsync(AuthResponse caller)
    {
        var response = await SendAsync(
            HttpMethod.Post, "/api/workspaces", caller, new { name = $"Workspace {Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Id(await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    private async Task<JsonElement> CreateProjectOkAsync(AuthResponse caller, Guid workspaceId)
    {
        var key = "K" + Guid.NewGuid().ToString("N")[..7].ToUpperInvariant();
        var response = await SendAsync(
            HttpMethod.Post, $"/api/workspaces/{workspaceId}/projects", caller, new { name = "Project", key });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<Guid> AddProjectMemberOkAsync(AuthResponse owner, Guid projectId, Guid userId)
    {
        var response = await SendAsync(
            HttpMethod.Post, $"/api/projects/{projectId}/members", owner, new { userId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Id(await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    /// <summary>Seeded through the aggregate, same reasoning as
    /// <c>ProjectEndpointTests.SeedWorkspaceMemberAsync</c>.</summary>
    private Task SeedWorkspaceMemberAsync(Guid workspaceId, Guid userId) =>
        api.QueryDbAsync(async db =>
        {
            var workspace = await db.Workspaces.Include(w => w.Members).SingleAsync(w => w.Id == workspaceId);
            workspace.AddMember(userId, WorkspaceRole.Member, DateTimeOffset.UtcNow);
            return await db.SaveChangesAsync();
        });

    private static async Task<List<JsonElement>> ReadMembersAsync(HttpResponseMessage response) =>
        [.. (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()];

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
}
