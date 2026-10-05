using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DevHub.Api.IntegrationTests.Harness;
using DevHub.Application.Auth.Contracts;
using DevHub.Application.Workspaces.Access;
using DevHub.Domain.Projects;
using DevHub.Domain.Workspaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevHub.Api.IntegrationTests.Workspaces;

/// <summary>
/// DEVHUB-026: <see cref="IWorkspaceAccessService"/> against a real PostgreSQL. One section per
/// resolver; each later resolver (DEVHUB-030, 036, 062, 066, 072, 077) adds its own, and each must
/// prove that a user from another workspace gets null — the 404 — for that resource type.
/// </summary>
public sealed class WorkspaceAccessServiceTests(DevHubApiFactory api) : IClassFixture<DevHubApiFactory>
{
    private readonly HttpClient _client = api.CreateClient();

    // ---- ForWorkspaceAsync -----------------------------------------------------------------

    [Fact]
    public async Task ForWorkspace_returns_the_owners_access()
    {
        var owner = await RegisterAsync();
        var workspaceId = await CreateWorkspaceAsync(owner);

        var access = await AsUserAsync(owner.User.Id, service => service.ForWorkspaceAsync(workspaceId, default));

        Assert.Equal(new WorkspaceAccess(workspaceId, null, WorkspaceRole.Owner), access);
    }

    [Fact]
    public async Task ForWorkspace_returns_a_members_own_role()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var workspaceId = await CreateWorkspaceAsync(owner);
        await AddMemberAsync(workspaceId, member.User.Id);

        var access = await AsUserAsync(member.User.Id, service => service.ForWorkspaceAsync(workspaceId, default));

        Assert.Equal(WorkspaceRole.Member, access?.Role);
    }

    [Fact]
    public async Task ForWorkspace_returns_null_for_a_member_of_another_workspace()
    {
        var owner = await RegisterAsync();
        var outsider = await RegisterAsync();
        var workspaceId = await CreateWorkspaceAsync(owner);

        // The outsider is an owner — of a different workspace. Owning something elsewhere must
        // grant nothing here.
        await CreateWorkspaceAsync(outsider);

        var access = await AsUserAsync(outsider.User.Id, service => service.ForWorkspaceAsync(workspaceId, default));

        Assert.Null(access);
    }

    [Fact]
    public async Task ForWorkspace_returns_null_for_a_workspace_that_does_not_exist()
    {
        var caller = await RegisterAsync();

        var access = await AsUserAsync(caller.User.Id, service => service.ForWorkspaceAsync(Guid.CreateVersion7(), default));

        Assert.Null(access);
    }

    [Fact]
    public async Task ForWorkspace_answers_at_most_once_per_request()
    {
        var owner = await RegisterAsync();
        var workspaceId = await CreateWorkspaceAsync(owner);

        var (first, second) = await AsUserAsync(owner.User.Id, async service =>
        {
            var before = await service.ForWorkspaceAsync(workspaceId, default);

            // Removed behind the service's back, mid-"request". A second query would see it gone;
            // the cached answer does not. This is the cache's whole contract: one request, one answer.
            await api.QueryDbAsync(db => db.Set<WorkspaceMember>()
                .Where(member => member.WorkspaceId == workspaceId)
                .ExecuteDeleteAsync());

            return (before, await service.ForWorkspaceAsync(workspaceId, default));
        });

        Assert.NotNull(first);
        Assert.Same(first, second);

        // A new request — a new scope — asks the database again.
        Assert.Null(await AsUserAsync(owner.User.Id, service => service.ForWorkspaceAsync(workspaceId, default)));
    }

    // ---- ForProjectAsync --------------------------------------------------------------------

    [Fact]
    public async Task ForProject_returns_the_creators_workspace_role()
    {
        var owner = await RegisterAsync();
        var workspaceId = await CreateWorkspaceAsync(owner);
        var projectId = await CreateProjectAsync(workspaceId, owner.User.Id);

        var access = await AsUserAsync(owner.User.Id, service => service.ForProjectAsync(projectId, default));

        Assert.Equal(new WorkspaceAccess(workspaceId, projectId, WorkspaceRole.Owner), access);
    }

    [Fact]
    public async Task ForProject_returns_a_members_own_workspace_role()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var workspaceId = await CreateWorkspaceAsync(owner);
        await AddMemberAsync(workspaceId, member.User.Id);
        var projectId = await CreateProjectAsync(workspaceId, owner.User.Id);

        var access = await AsUserAsync(member.User.Id, service => service.ForProjectAsync(projectId, default));

        Assert.Equal(WorkspaceRole.Member, access?.Role);
        Assert.Equal(projectId, access?.ProjectId);
    }

    [Fact]
    public async Task ForProject_returns_null_for_a_member_of_another_workspace()
    {
        var owner = await RegisterAsync();
        var outsider = await RegisterAsync();
        var workspaceId = await CreateWorkspaceAsync(owner);
        var projectId = await CreateProjectAsync(workspaceId, owner.User.Id);

        // The outsider is a member — of a different workspace. Owning a workspace elsewhere
        // must grant nothing here.
        await CreateWorkspaceAsync(outsider);

        var access = await AsUserAsync(outsider.User.Id, service => service.ForProjectAsync(projectId, default));

        Assert.Null(access);
    }

    [Fact]
    public async Task ForProject_returns_null_for_a_project_that_does_not_exist()
    {
        var caller = await RegisterAsync();

        var access = await AsUserAsync(caller.User.Id, service => service.ForProjectAsync(Guid.CreateVersion7(), default));

        Assert.Null(access);
    }

    // ---- Helpers ---------------------------------------------------------------------------

    /// <summary>
    /// Runs <paramref name="act"/> as one "request" of <paramref name="userId"/>: a fresh DI scope
    /// with an HttpContext carrying the <c>sub</c> claim, which is exactly what ICurrentUser reads.
    /// </summary>
    private async Task<T> AsUserAsync<T>(Guid userId, Func<IWorkspaceAccessService, Task<T>> act)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", userId.ToString())], "Test")),
        };

        try
        {
            return await act(scope.ServiceProvider.GetRequiredService<IWorkspaceAccessService>());
        }
        finally
        {
            accessor.HttpContext = null;
        }
    }

    private Task<AuthResponse> RegisterAsync() => _client.RegisterOkAsync(AuthApi.UniqueEmail());

    private async Task<Guid> CreateWorkspaceAsync(AuthResponse owner)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/workspaces")
        {
            Content = JsonContent.Create(new { name = "Access", slug = $"ws-{Guid.NewGuid():N}" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    /// <summary>Seeded through the aggregate: adding members over HTTP is DEVHUB-025.</summary>
    private Task AddMemberAsync(Guid workspaceId, Guid userId) =>
        api.QueryDbAsync(async db =>
        {
            var workspace = await db.Workspaces.Include(w => w.Members).SingleAsync(w => w.Id == workspaceId);
            workspace.AddMember(userId, WorkspaceRole.Member, DateTimeOffset.UtcNow);
            return await db.SaveChangesAsync();
        });

    /// <summary>Seeded through the aggregate: project endpoints are DEVHUB-031.</summary>
    private Task<Guid> CreateProjectAsync(Guid workspaceId, Guid creatorId) =>
        api.QueryDbAsync(async db =>
        {
            var key = $"PRJ{Guid.NewGuid():N}"[..10].ToUpperInvariant();
            var project = Project.Create(workspaceId, "Access", key, creatorId, DateTimeOffset.UtcNow);
            db.Add(project);
            await db.SaveChangesAsync();
            return project.Id;
        });
}
