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
/// DEVHUB-024: <c>/api/workspaces</c> (api-endpoints.md §2). The first resource scoped to its
/// members, so these tests are the pattern every later resource copies: a stranger gets 404,
/// a member without the role gets 403, and neither ever sees the other's data.
/// </summary>
public sealed class WorkspaceEndpointTests(DevHubApiFactory api) : IClassFixture<DevHubApiFactory>
{
    private const string Errors = "https://devhub.dev/errors/";

    private readonly HttpClient _client = api.CreateClient();

    // ---- Scoping: the acceptance criteria -------------------------------------------------

    [Fact]
    public async Task The_list_contains_only_the_callers_own_workspaces()
    {
        var alice = await RegisterAsync();
        var bob = await RegisterAsync();
        var aliceFirst = await CreateOkAsync(alice, UniqueName("Zeta"));
        var aliceSecond = await CreateOkAsync(alice, UniqueName("Alpha"));
        var bobs = await CreateOkAsync(bob, UniqueName("Bob"));

        var aliceList = await ListOkAsync(alice);
        var bobList = await ListOkAsync(bob);

        // Ordered by name: "Alpha …" before "Zeta …".
        Assert.Equal(new[] { Id(aliceSecond), Id(aliceFirst) }, aliceList.Select(Id));
        Assert.Equal(new[] { Id(bobs) }, bobList.Select(Id));
    }

    [Fact]
    public async Task A_non_member_gets_404_for_get_identical_to_a_workspace_that_does_not_exist()
    {
        var owner = await RegisterAsync();
        var stranger = await RegisterAsync();
        var workspace = await CreateOkAsync(owner, UniqueName());

        var existing = await SendAsync(HttpMethod.Get, $"/api/workspaces/{Id(workspace)}", stranger);
        var missing = await SendAsync(HttpMethod.Get, $"/api/workspaces/{Guid.CreateVersion7()}", stranger);

        // Same status, same type: nothing in the response tells the two apart.
        await AssertProblemAsync(existing, HttpStatusCode.NotFound, "workspaces.not_found");
        await AssertProblemAsync(missing, HttpStatusCode.NotFound, "workspaces.not_found");
    }

    [Fact]
    public async Task A_non_member_gets_404_for_patch_never_403_and_nothing_changes()
    {
        var owner = await RegisterAsync();
        var stranger = await RegisterAsync();
        var workspace = await CreateOkAsync(owner, "Original Name " + Guid.NewGuid().ToString("N")[..8]);

        var response = await SendAsync(HttpMethod.Patch, $"/api/workspaces/{Id(workspace)}", stranger, new { name = "Hijacked" });

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "workspaces.not_found");
        Assert.Equal(workspace.GetProperty("name").GetString(), (await LoadAsync(Id(workspace))).Name);
    }

    [Fact]
    public async Task A_member_who_is_not_an_owner_gets_403_on_patch_and_nothing_changes()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var workspace = await CreateOkAsync(owner, UniqueName());
        await AddMemberAsync(Id(workspace), member.User.Id);

        var response = await SendAsync(HttpMethod.Patch, $"/api/workspaces/{Id(workspace)}", member, new { name = "Renamed" });

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "workspaces.owner_required");
        Assert.Equal(workspace.GetProperty("name").GetString(), (await LoadAsync(Id(workspace))).Name);
    }

    [Fact]
    public async Task A_member_sees_the_workspace_with_their_own_role_and_the_member_count()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var workspace = await CreateOkAsync(owner, UniqueName());
        await AddMemberAsync(Id(workspace), member.User.Id);

        var single = await SendAsync(HttpMethod.Get, $"/api/workspaces/{Id(workspace)}", member);
        var listed = Assert.Single(await ListOkAsync(member));

        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
        foreach (var dto in new[] { await single.Content.ReadFromJsonAsync<JsonElement>(), listed })
        {
            Assert.Equal(Id(workspace), Id(dto));
            Assert.Equal("Member", dto.GetProperty("role").GetString());
            Assert.Equal(2, dto.GetProperty("memberCount").GetInt32());
        }

        // The owner, looking at the same workspace, sees their own role — not the member's.
        Assert.Equal("Owner", Assert.Single(await ListOkAsync(owner)).GetProperty("role").GetString());
    }

    // ---- Create ---------------------------------------------------------------------------

    [Fact]
    public async Task Create_returns_201_with_the_caller_as_the_only_owner_and_a_derived_slug()
    {
        var caller = await RegisterAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var response = await SendAsync(HttpMethod.Post, "/api/workspaces", caller, new { name = $"  Café Élite {suffix}  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"/api/workspaces/{Id(body)}", response.Headers.Location?.OriginalString);
        Assert.Equal($"Café Élite {suffix}", body.GetProperty("name").GetString());
        Assert.Equal($"cafe-elite-{suffix}", body.GetProperty("slug").GetString());
        Assert.Equal("Owner", body.GetProperty("role").GetString());
        Assert.Equal(1, body.GetProperty("memberCount").GetInt32());
        Assert.NotEqual(default, body.GetProperty("createdAt").GetDateTimeOffset());

        var saved = await LoadAsync(Id(body));
        var owner = Assert.Single(saved.Members);
        Assert.Equal(caller.User.Id, owner.UserId);
        Assert.Equal(WorkspaceRole.Owner, owner.Role);
    }

    [Fact]
    public async Task Create_uses_an_explicit_slug_as_given()
    {
        var caller = await RegisterAsync();
        var slug = UniqueSlug();

        var body = await CreateOkAsync(caller, "Anything At All", slug);

        Assert.Equal(slug, body.GetProperty("slug").GetString());
    }

    [Fact]
    public async Task A_duplicate_slug_returns_409_even_across_users()
    {
        var first = await RegisterAsync();
        var second = await RegisterAsync();
        var slug = UniqueSlug();
        await CreateOkAsync(first, "First", slug);

        var response = await SendAsync(HttpMethod.Post, "/api/workspaces", second, new { name = "Second", slug });

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "workspaces.slug_taken");
        Assert.Empty(await ListOkAsync(second));
    }

    [Fact]
    public async Task A_derived_slug_that_collides_returns_409()
    {
        var caller = await RegisterAsync();
        var name = UniqueName();
        await CreateOkAsync(caller, name);

        var response = await SendAsync(HttpMethod.Post, "/api/workspaces", caller, new { name });

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "workspaces.slug_taken");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Create_without_a_name_returns_400_on_name(string? name)
    {
        var caller = await RegisterAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/workspaces", caller, new { name, slug = UniqueSlug() });

        await AssertValidationErrorAsync(response, "name");
    }

    [Fact]
    public async Task Create_with_a_name_over_80_characters_returns_400_on_name()
    {
        var caller = await RegisterAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/workspaces", caller, new { name = new string('a', 81) });

        await AssertValidationErrorAsync(response, "name");
    }

    [Theory]
    [InlineData("Acme")]
    [InlineData("ab")]
    [InlineData("acme--corp")]
    [InlineData("")]
    public async Task Create_with_an_invalid_slug_returns_400_on_slug(string slug)
    {
        var caller = await RegisterAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/workspaces", caller, new { name = "Acme", slug });

        await AssertValidationErrorAsync(response, "slug");
    }

    [Fact]
    public async Task A_name_with_nothing_to_derive_a_slug_from_returns_400_on_slug()
    {
        var caller = await RegisterAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/workspaces", caller, new { name = "東京チーム" });

        await AssertValidationErrorAsync(response, "slug");
    }

    // ---- Update ---------------------------------------------------------------------------

    [Fact]
    public async Task An_owner_can_rename_and_the_slug_never_changes()
    {
        var owner = await RegisterAsync();
        var slug = UniqueSlug();
        var workspace = await CreateOkAsync(owner, "Before", slug);

        // A slug in the body is not an error, and not applied: there is no field to bind it to.
        var response = await SendAsync(
            HttpMethod.Patch, $"/api/workspaces/{Id(workspace)}", owner, new { name = "  After  ", slug = "hijacked-slug" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("After", body.GetProperty("name").GetString());
        Assert.Equal(slug, body.GetProperty("slug").GetString());
        Assert.Equal("Owner", body.GetProperty("role").GetString());

        var saved = await LoadAsync(Id(workspace));
        Assert.Equal("After", saved.Name);
        Assert.Equal(slug, saved.Slug);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Patch_without_a_name_returns_400(string? name)
    {
        var owner = await RegisterAsync();
        var workspace = await CreateOkAsync(owner, UniqueName());

        var response = await SendAsync(HttpMethod.Patch, $"/api/workspaces/{Id(workspace)}", owner, new { name });

        await AssertValidationErrorAsync(response, "name");
    }

    // ---- Authentication and /me -----------------------------------------------------------

    [Theory]
    [InlineData("POST", "/api/workspaces")]
    [InlineData("GET", "/api/workspaces")]
    [InlineData("GET", "/api/workspaces/0199a000-0000-7000-8000-000000000000")]
    [InlineData("PATCH", "/api/workspaces/0199a000-0000-7000-8000-000000000000")]
    public async Task Every_endpoint_returns_401_without_a_token(string method, string url)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), url) { Content = JsonContent.Create(new { name = "X" }) };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_lists_the_callers_workspaces_with_their_role()
    {
        var caller = await RegisterAsync();
        var other = await RegisterAsync();
        var owned = await CreateOkAsync(caller, UniqueName("Alpha"));
        var joined = await CreateOkAsync(other, UniqueName("Beta"));
        await CreateOkAsync(other, UniqueName("Not Mine"));
        await AddMemberAsync(Id(joined), caller.User.Id);

        var response = await SendAsync(HttpMethod.Get, "/api/me", caller);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var workspaces = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("workspaces");
        Assert.Collection(
            workspaces.EnumerateArray(),
            first =>
            {
                Assert.Equal(Id(owned), Id(first));
                Assert.Equal(owned.GetProperty("slug").GetString(), first.GetProperty("slug").GetString());
                Assert.Equal("Owner", first.GetProperty("role").GetString());
            },
            second =>
            {
                Assert.Equal(Id(joined), Id(second));
                Assert.Equal("Member", second.GetProperty("role").GetString());
            });

        // /me keeps its own, smaller contract (WorkspaceSummaryDto).
        Assert.False(workspaces[0].TryGetProperty("memberCount", out _));
    }

    // ---- Helpers --------------------------------------------------------------------------

    private static string UniqueName(string prefix = "Workspace") => $"{prefix} {Guid.NewGuid():N}"[..Math.Min(prefix.Length + 9, 80)];

    private static string UniqueSlug() => $"ws-{Guid.NewGuid():N}";

    private static Guid Id(JsonElement workspace) => workspace.GetProperty("id").GetGuid();

    private Task<AuthResponse> RegisterAsync() => _client.RegisterOkAsync(AuthApi.UniqueEmail());

    private async Task<JsonElement> CreateOkAsync(AuthResponse caller, string name, string? slug = null)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/workspaces", caller, new { name, slug });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<List<JsonElement>> ListOkAsync(AuthResponse caller)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/workspaces", caller);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return [.. (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()];
    }

    /// <summary>
    /// Seeded through the aggregate, not an endpoint: adding members is DEVHUB-025. Going through
    /// <see cref="Workspace.AddMember"/> still applies every domain rule a real request would.
    /// </summary>
    private Task AddMemberAsync(Guid workspaceId, Guid userId) =>
        api.QueryDbAsync(async db =>
        {
            var workspace = await db.Workspaces.Include(w => w.Members).SingleAsync(w => w.Id == workspaceId);
            workspace.AddMember(userId, WorkspaceRole.Member, DateTimeOffset.UtcNow);
            return await db.SaveChangesAsync();
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
