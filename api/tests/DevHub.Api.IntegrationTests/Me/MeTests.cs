using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DevHub.Api.IntegrationTests.Harness;
using DevHub.Application.Auth.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;

namespace DevHub.Api.IntegrationTests.Me;

/// <summary>DEVHUB-019: <c>GET</c>/<c>PATCH /api/me</c> (api-endpoints.md §1).</summary>
public sealed class MeTests(DevHubApiFactory api) : IClassFixture<DevHubApiFactory>
{
    private const string AvatarKey = "avatars/seeded/avatar.png";

    private readonly HttpClient _client = api.CreateClient();

    [Fact]
    public async Task Get_without_a_token_returns_401()
    {
        var response = await _client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Patch_without_a_token_returns_401()
    {
        var response = await _client.PatchAsJsonAsync("/api/me", new { displayName = "Mallory" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_returns_the_callers_profile_and_workspaces()
    {
        var email = AuthApi.UniqueEmail();
        var registered = await _client.RegisterOkAsync(email);

        var response = await SendAsync(HttpMethod.Get, registered);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(registered.User.Id, me.GetProperty("id").GetGuid());
        Assert.Equal(email, me.GetProperty("email").GetString());
        Assert.Equal("Ada", me.GetProperty("displayName").GetString());
        Assert.Equal(JsonValueKind.Null, me.GetProperty("avatarUrl").ValueKind);

        // Present and empty until workspaces exist (DEVHUB-024 fills it).
        Assert.Equal(0, me.GetProperty("workspaces").GetArrayLength());
    }

    [Fact]
    public async Task Responses_never_include_the_password_hash_or_token_values()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var get = await (await SendAsync(HttpMethod.Get, registered)).Content.ReadAsStringAsync();
        var patch = await (await SendAsync(HttpMethod.Patch, registered, new { displayName = "Grace" }))
            .Content.ReadAsStringAsync();

        foreach (var body in new[] { get, patch })
        {
            Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(registered.RefreshToken, body, StringComparison.Ordinal);
            Assert.DoesNotContain(registered.AccessToken, body, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Patching_only_the_display_name_does_not_clear_the_avatar()
    {
        var registered = await RegisterWithAvatarAsync();

        var response = await SendAsync(HttpMethod.Patch, registered, new { displayName = "Grace" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await LoadUserAsync(registered);
        Assert.Equal("Grace", user.DisplayName);
        Assert.Equal(AvatarKey, user.AvatarKey);
    }

    [Fact]
    public async Task An_explicit_null_avatar_removes_it_and_leaves_the_name()
    {
        var registered = await RegisterWithAvatarAsync();

        var response = await SendAsync(HttpMethod.Patch, registered, new { avatarAttachmentId = (Guid?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await LoadUserAsync(registered);
        Assert.Null(user.AvatarKey);
        Assert.Equal("Ada", user.DisplayName);
    }

    [Fact]
    public async Task An_empty_patch_changes_nothing()
    {
        var registered = await RegisterWithAvatarAsync();
        var before = await LoadUserAsync(registered);

        var response = await SendAsync(HttpMethod.Patch, registered, new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var after = await LoadUserAsync(registered);
        Assert.Equal(before.DisplayName, after.DisplayName);
        Assert.Equal(before.AvatarKey, after.AvatarKey);
        Assert.Equal(before.UpdatedAt, after.UpdatedAt);
    }

    [Fact]
    public async Task The_display_name_is_trimmed_and_returned()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var response = await SendAsync(HttpMethod.Patch, registered, new { displayName = "  Grace Hopper  " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Grace Hopper", body.GetProperty("displayName").GetString());
        Assert.Equal("Grace Hopper", (await LoadUserAsync(registered)).DisplayName);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(0)]
    public async Task A_display_name_outside_1_to_100_characters_returns_400(int length)
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var response = await SendAsync(HttpMethod.Patch, registered, new { displayName = new string('a', length) });

        await AssertDisplayNameErrorAsync(response);
    }

    [Fact]
    public async Task One_hundred_characters_is_accepted_after_trimming()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var response = await SendAsync(HttpMethod.Patch, registered, new { displayName = $"  {new string('a', 100)}  " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_null_display_name_returns_400_because_a_user_always_has_a_name()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var response = await SendAsync(HttpMethod.Patch, registered, new { displayName = (string?)null });

        await AssertDisplayNameErrorAsync(response);
    }

    [Fact]
    public async Task An_avatar_attachment_id_returns_404_until_attachments_exist()
    {
        var registered = await RegisterWithAvatarAsync();

        var response = await SendAsync(HttpMethod.Patch, registered, new { displayName = "Grace", avatarAttachmentId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://devhub.dev/errors/attachments.not_found", problem.GetProperty("type").GetString());

        // Nothing in the request was applied — not even the valid name.
        var user = await LoadUserAsync(registered);
        Assert.Equal("Ada", user.DisplayName);
        Assert.Equal(AvatarKey, user.AvatarKey);
    }

    [Fact]
    public async Task A_valid_token_for_a_deleted_user_returns_401()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());
        await api.QueryDbAsync(db => db.Users.Where(user => user.Id == registered.User.Id).ExecuteDeleteAsync());

        var get = await SendAsync(HttpMethod.Get, registered);
        var patch = await SendAsync(HttpMethod.Patch, registered, new { displayName = "Ghost" });

        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, patch.StatusCode);
    }

    [Fact]
    public void Swagger_describes_optional_fields_as_their_plain_type()
    {
        var document = api.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        var properties = document.Components!.Schemas!["UpdateMeCommand"].Properties!;

        Assert.True(properties["displayName"].Type!.Value.HasFlag(JsonSchemaType.String));
        Assert.True(properties["avatarAttachmentId"].Type!.Value.HasFlag(JsonSchemaType.String));
        Assert.Equal("uuid", properties["avatarAttachmentId"].Format);
    }

    private async Task<AuthResponse> RegisterWithAvatarAsync()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        // No API can set an avatar before DEVHUB-090, so the row is seeded directly.
        await api.QueryDbAsync(db => db.Users
            .Where(user => user.Id == registered.User.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.AvatarKey, AvatarKey)));

        return registered;
    }

    private Task<Domain.Users.User> LoadUserAsync(AuthResponse registered) =>
        api.QueryDbAsync(db => db.Users.AsNoTracking().SingleAsync(user => user.Id == registered.User.Id));

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, AuthResponse caller, object? body = null)
    {
        using var request = new HttpRequestMessage(method, "/api/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", caller.AccessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private static async Task AssertDisplayNameErrorAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://devhub.dev/errors/validation", problem.GetProperty("type").GetString());
        Assert.True(problem.GetProperty("errors").TryGetProperty("displayName", out _));
    }
}
