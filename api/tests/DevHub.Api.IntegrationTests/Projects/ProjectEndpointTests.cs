using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DevHub.Api.IntegrationTests.Harness;
using DevHub.Application.Auth.Contracts;
using DevHub.Domain.Projects;
using DevHub.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace DevHub.Api.IntegrationTests.Projects;

/// <summary>
/// DEVHUB-031: <c>/api/workspaces/{workspaceId}/projects</c> and <c>/api/projects/{projectId}</c>
/// (api-endpoints.md §3). Scoping follows the same pattern as
/// <see cref="Workspaces.WorkspaceEndpointTests"/>: a stranger gets 404, a member who is not an
/// owner gets 403 on a mutation, and the key — unlike a workspace's slug — is rejected outright if
/// a client tries to change it, rather than silently ignored.
/// </summary>
public sealed class ProjectEndpointTests(DevHubApiFactory api) : IClassFixture<DevHubApiFactory>
{
    private const string Errors = "https://devhub.dev/errors/";

    private readonly HttpClient _client = api.CreateClient();

    // ---- Create ---------------------------------------------------------------------------

    [Fact]
    public async Task Create_returns_201_with_a_location_header_and_the_caller_as_the_only_member()
    {
        var owner = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);

        var response = await SendAsync(HttpMethod.Post, $"/api/workspaces/{workspaceId}/projects", owner,
            new { name = "DevHub API", key = UniqueKey(), description = "Backend.", color = "#4F46E5", icon = "rocket" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"/api/projects/{Id(body)}", response.Headers.Location?.OriginalString);
        Assert.Equal("DevHub API", body.GetProperty("name").GetString());
        Assert.Equal("Backend.", body.GetProperty("description").GetString());
        Assert.Equal("#4F46E5", body.GetProperty("color").GetString());
        Assert.Equal("rocket", body.GetProperty("icon").GetString());

        var saved = await LoadAsync(Id(body));
        var member = Assert.Single(saved.Members);
        Assert.Equal(owner.User.Id, member.UserId);
    }

    [Fact]
    public async Task A_duplicate_key_in_the_same_workspace_returns_409()
    {
        var owner = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        var key = UniqueKey();
        await CreateProjectOkAsync(owner, workspaceId, key);

        var response = await SendAsync(
            HttpMethod.Post, $"/api/workspaces/{workspaceId}/projects", owner, new { name = "Another", key });

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "projects.key_taken");
    }

    [Fact]
    public async Task The_same_key_is_allowed_in_a_different_workspace()
    {
        var owner = await RegisterAsync();
        var workspaceA = await CreateWorkspaceOkAsync(owner);
        var workspaceB = await CreateWorkspaceOkAsync(owner);
        var key = UniqueKey();
        await CreateProjectOkAsync(owner, workspaceA, key);

        var response = await SendAsync(
            HttpMethod.Post, $"/api/workspaces/{workspaceB}/projects", owner, new { name = "Another", key });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task A_non_member_cannot_create_a_project_and_gets_404()
    {
        var owner = await RegisterAsync();
        var stranger = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);

        var response = await SendAsync(
            HttpMethod.Post, $"/api/workspaces/{workspaceId}/projects", stranger, new { name = "X", key = UniqueKey() });

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "workspaces.not_found");
    }

    [Theory]
    [InlineData("dev")]
    [InlineData("DEV-1")]
    [InlineData("A")]
    public async Task Create_with_an_invalid_key_returns_400(string key)
    {
        var owner = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);

        var response = await SendAsync(HttpMethod.Post, $"/api/workspaces/{workspaceId}/projects", owner, new { name = "X", key });

        await AssertValidationErrorAsync(response, "key");
    }

    // ---- List -------------------------------------------------------------------------------

    [Fact]
    public async Task List_excludes_archived_projects_by_default_but_includes_them_when_asked()
    {
        var owner = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        var active = await CreateProjectOkAsync(owner, workspaceId, UniqueKey());
        var archived = await CreateProjectOkAsync(owner, workspaceId, UniqueKey());
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Delete, $"/api/projects/{Id(archived)}", owner)).StatusCode);

        var defaultList = await ListOkAsync(workspaceId, owner);
        var fullList = await ListOkAsync(workspaceId, owner, includeArchived: true);

        Assert.Equal(new[] { Id(active) }, defaultList.Select(Id));
        Assert.Equal(
            new[] { Id(active), Id(archived) }.Order(),
            fullList.Select(Id).Order());
    }

    [Fact]
    public async Task A_non_member_gets_404_for_list_identical_to_a_workspace_that_does_not_exist()
    {
        var owner = await RegisterAsync();
        var stranger = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);

        var existing = await SendAsync(HttpMethod.Get, $"/api/workspaces/{workspaceId}/projects", stranger);
        var missing = await SendAsync(HttpMethod.Get, $"/api/workspaces/{Guid.CreateVersion7()}/projects", stranger);

        await AssertProblemAsync(existing, HttpStatusCode.NotFound, "workspaces.not_found");
        await AssertProblemAsync(missing, HttpStatusCode.NotFound, "workspaces.not_found");
    }

    // ---- Get --------------------------------------------------------------------------------

    [Fact]
    public async Task A_non_member_gets_404_for_get_identical_to_a_project_that_does_not_exist()
    {
        var owner = await RegisterAsync();
        var stranger = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        var project = await CreateProjectOkAsync(owner, workspaceId, UniqueKey());

        var existing = await SendAsync(HttpMethod.Get, $"/api/projects/{Id(project)}", stranger);
        var missing = await SendAsync(HttpMethod.Get, $"/api/projects/{Guid.CreateVersion7()}", stranger);

        await AssertProblemAsync(existing, HttpStatusCode.NotFound, "projects.not_found");
        await AssertProblemAsync(missing, HttpStatusCode.NotFound, "projects.not_found");
    }

    [Fact]
    public async Task Any_workspace_member_can_get_a_project_not_just_its_creator()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        await SeedWorkspaceMemberAsync(workspaceId, member.User.Id);
        var project = await CreateProjectOkAsync(owner, workspaceId, UniqueKey());

        var response = await SendAsync(HttpMethod.Get, $"/api/projects/{Id(project)}", member);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---- Update -----------------------------------------------------------------------------

    [Fact]
    public async Task An_owner_can_rename_and_the_key_never_changes()
    {
        var owner = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        var key = UniqueKey();
        var project = await CreateProjectOkAsync(owner, workspaceId, key, name: "Before");

        var response = await SendAsync(HttpMethod.Patch, $"/api/projects/{Id(project)}", owner, new { name = "After" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("After", body.GetProperty("name").GetString());
        Assert.Equal(key, body.GetProperty("key").GetString());
        Assert.Equal(key, (await LoadAsync(Id(project))).Key);
    }

    [Fact]
    public async Task Patch_with_only_color_leaves_the_existing_icon_untouched()
    {
        var owner = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        var project = await CreateProjectOkAsync(owner, workspaceId, UniqueKey(), color: "#111111", icon: "rocket");

        var response = await SendAsync(HttpMethod.Patch, $"/api/projects/{Id(project)}", owner, new { color = "#222222" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("#222222", body.GetProperty("color").GetString());
        Assert.Equal("rocket", body.GetProperty("icon").GetString());
    }

    [Fact]
    public async Task Patch_with_a_key_returns_422_and_changes_nothing()
    {
        var owner = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        var key = UniqueKey();
        var project = await CreateProjectOkAsync(owner, workspaceId, key);

        var response = await SendAsync(HttpMethod.Patch, $"/api/projects/{Id(project)}", owner, new { key = "NEWKEY" });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "projects.key_immutable");
        Assert.Equal(key, (await LoadAsync(Id(project))).Key);
    }

    [Fact]
    public async Task A_non_member_gets_404_for_patch_never_403_and_nothing_changes()
    {
        var owner = await RegisterAsync();
        var stranger = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        var project = await CreateProjectOkAsync(owner, workspaceId, UniqueKey(), name: "Original");

        var response = await SendAsync(HttpMethod.Patch, $"/api/projects/{Id(project)}", stranger, new { name = "Hijacked" });

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "projects.not_found");
        Assert.Equal("Original", (await LoadAsync(Id(project))).Name);
    }

    [Fact]
    public async Task A_member_who_is_not_an_owner_gets_403_on_patch_and_nothing_changes()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        await SeedWorkspaceMemberAsync(workspaceId, member.User.Id);
        var project = await CreateProjectOkAsync(owner, workspaceId, UniqueKey(), name: "Original");

        var response = await SendAsync(HttpMethod.Patch, $"/api/projects/{Id(project)}", member, new { name = "Hijacked" });

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "workspaces.owner_required");
        Assert.Equal("Original", (await LoadAsync(Id(project))).Name);
    }

    // ---- Archive ----------------------------------------------------------------------------

    [Fact]
    public async Task An_owner_can_archive_a_project_and_it_is_then_hidden_from_the_default_list()
    {
        var owner = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        var project = await CreateProjectOkAsync(owner, workspaceId, UniqueKey());

        var response = await SendAsync(HttpMethod.Delete, $"/api/projects/{Id(project)}", owner);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull((await LoadAsync(Id(project))).ArchivedAt);
        Assert.DoesNotContain(Id(project), (await ListOkAsync(workspaceId, owner)).Select(Id));
    }

    [Fact]
    public async Task A_member_who_is_not_an_owner_gets_403_on_archive_and_nothing_changes()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var workspaceId = await CreateWorkspaceOkAsync(owner);
        await SeedWorkspaceMemberAsync(workspaceId, member.User.Id);
        var project = await CreateProjectOkAsync(owner, workspaceId, UniqueKey());

        var response = await SendAsync(HttpMethod.Delete, $"/api/projects/{Id(project)}", member);

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "workspaces.owner_required");
        Assert.Null((await LoadAsync(Id(project))).ArchivedAt);
    }

    // ---- Helpers --------------------------------------------------------------------------

    private static string UniqueKey() => "K" + Guid.NewGuid().ToString("N")[..7].ToUpperInvariant();

    private static Guid Id(JsonElement element) => element.GetProperty("id").GetGuid();

    private Task<AuthResponse> RegisterAsync() => _client.RegisterOkAsync(AuthApi.UniqueEmail());

    private async Task<Guid> CreateWorkspaceOkAsync(AuthResponse caller)
    {
        var response = await SendAsync(
            HttpMethod.Post, "/api/workspaces", caller, new { name = $"Workspace {Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Id(await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    private async Task<JsonElement> CreateProjectOkAsync(
        AuthResponse caller, Guid workspaceId, string key,
        string name = "Project", string? description = null, string? color = null, string? icon = null)
    {
        var response = await SendAsync(
            HttpMethod.Post, $"/api/workspaces/{workspaceId}/projects", caller, new { name, key, description, color, icon });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<List<JsonElement>> ListOkAsync(Guid workspaceId, AuthResponse caller, bool includeArchived = false)
    {
        var response = await SendAsync(
            HttpMethod.Get, $"/api/workspaces/{workspaceId}/projects?includeArchived={includeArchived}", caller);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return [.. (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()];
    }

    /// <summary>Seeded through the aggregate, same reasoning as
    /// <c>WorkspaceEndpointTests.AddMemberAsync</c>: applies every domain rule a real invite
    /// would, without needing a second real user account to invite by email.</summary>
    private Task SeedWorkspaceMemberAsync(Guid workspaceId, Guid userId) =>
        api.QueryDbAsync(async db =>
        {
            var workspace = await db.Workspaces.Include(w => w.Members).SingleAsync(w => w.Id == workspaceId);
            workspace.AddMember(userId, WorkspaceRole.Member, DateTimeOffset.UtcNow);
            return await db.SaveChangesAsync();
        });

    private Task<Project> LoadAsync(Guid projectId) =>
        api.QueryDbAsync(db => db.Projects.AsNoTracking().Include(p => p.Members).SingleAsync(p => p.Id == projectId));

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
