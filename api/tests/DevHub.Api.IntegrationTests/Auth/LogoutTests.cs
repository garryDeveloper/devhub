using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DevHub.Api.IntegrationTests.Harness;
using DevHub.Application.Auth.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DevHub.Api.IntegrationTests.Auth;

/// <summary>DEVHUB-017: logout revokes the session and never reveals token validity (auth-spec.md §4).</summary>
public sealed class LogoutTests(DevHubApiFactory api) : IClassFixture<DevHubApiFactory>
{
    private readonly HttpClient _client = api.CreateClient();

    [Fact]
    public async Task After_logout_the_refresh_token_returns_401()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var logout = await _client.LogoutAsync(registered.RefreshToken);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await _client.RefreshAsync(registered.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Logging_out_twice_still_returns_204()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var first = await _client.LogoutAsync(registered.RefreshToken);
        var second = await _client.LogoutAsync(registered.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    [Theory]
    [InlineData("not-a-token-we-ever-issued")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Garbage_or_a_missing_token_returns_204_not_400(string? refreshToken)
    {
        var response = await _client.LogoutAsync(refreshToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task An_empty_body_returns_204()
    {
        using var content = new StringContent("", null, "application/json");

        var response = await _client.PostAsync("/api/auth/logout", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_with_a_stale_rotated_token_still_ends_the_session()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());
        var head = await _client.RefreshOkAsync(registered.RefreshToken);

        // A second tab still holding the original token logs out.
        var logout = await _client.LogoutAsync(registered.RefreshToken);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await _client.RefreshAsync(head.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Logout_ends_only_that_session_not_the_users_other_devices()
    {
        var email = AuthApi.UniqueEmail();
        var registered = await _client.RegisterOkAsync(email);
        var otherDevice = (await (await _client.LoginAsync(email, AuthApi.ValidPassword))
            .Content.ReadFromJsonAsync<AuthResponse>())!;

        await _client.LogoutAsync(registered.RefreshToken);

        var other = await _client.RefreshAsync(otherDevice.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, other.StatusCode);

        var active = await api.QueryDbAsync(db => db.RefreshTokens
            .CountAsync(token => token.UserId == registered.User.Id && token.RevokedAt == null));
        Assert.Equal(1, active);
    }

    [Fact]
    public async Task Logout_does_not_need_an_access_token()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        // No Authorization header — the case of a client whose access token already expired.
        Assert.Null(_client.DefaultRequestHeaders.Authorization);
        var response = await _client.LogoutAsync(registered.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>
    /// Pins the documented limit rather than hiding it: an access token issued before logout keeps
    /// working until it expires (≤ 15 min). If a denylist is ever added, this test must flip.
    /// </summary>
    [Fact]
    public async Task An_access_token_issued_before_logout_stays_valid_until_it_expires()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());
        await _client.LogoutAsync(registered.RefreshToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/whoami");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken);
        var whoami = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, whoami.StatusCode);
    }
}
