using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using DevHub.Api.IntegrationTests.Harness;
using DevHub.Application.Auth.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHub.Api.IntegrationTests.Auth;

/// <summary>DEVHUB-016: refresh token rotation and reuse detection (auth-spec.md §4).</summary>
public sealed class RefreshTests(DevHubApiFactory api) : IClassFixture<DevHubApiFactory>
{
    private readonly HttpClient _client = api.CreateClient();

    [Fact]
    public async Task A_refresh_returns_a_new_access_token_and_a_new_refresh_token()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var response = await _client.RefreshAsync(registered.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<RefreshResponse>())!;
        Assert.NotEqual(registered.RefreshToken, body.RefreshToken);
        Assert.NotEqual(registered.AccessToken, body.AccessToken);
        Assert.Equal(900, body.ExpiresIn);

        // The new access token is a real one, not just a new string.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/whoami");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        var whoami = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, whoami.StatusCode);
    }

    [Fact]
    public async Task The_rotated_token_can_itself_be_refreshed()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var first = await _client.RefreshOkAsync(registered.RefreshToken);
        var second = await _client.RefreshOkAsync(first.RefreshToken);

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
    }

    [Fact]
    public async Task The_previous_token_is_unusable_immediately_afterwards()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());
        await _client.RefreshOkAsync(registered.RefreshToken);

        var reused = await _client.RefreshAsync(registered.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
        var problem = (await reused.Content.ReadFromJsonAsync<ProblemDetails>())!;
        Assert.Equal("https://devhub.dev/errors/auth.invalid_refresh_token", problem.Type);
    }

    [Fact]
    public async Task Reusing_a_revoked_token_revokes_the_whole_session_but_not_other_sessions()
    {
        var email = AuthApi.UniqueEmail();
        var registered = await _client.RegisterOkAsync(email);

        // A second, independent session — another device.
        var otherDevice = (await (await _client.LoginAsync(email, AuthApi.ValidPassword))
            .Content.ReadFromJsonAsync<AuthResponse>())!;

        // The legitimate client rotates twice: R0 → R1 → R2.
        var r1 = await _client.RefreshOkAsync(registered.RefreshToken);
        var r2 = await _client.RefreshOkAsync(r1.RefreshToken);

        // An attacker replays the stolen R0.
        var replay = await _client.RefreshAsync(registered.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // The head of the chain — which was valid a moment ago — is now dead too.
        var head = await _client.RefreshAsync(r2.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, head.StatusCode);

        // Every row of that session is revoked, none of the other session's.
        var userId = registered.User.Id;
        var active = await api.QueryDbAsync(db => db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync());
        var session = Assert.Single(active);
        Assert.Equal(Hash(otherDevice.RefreshToken), session.TokenHash);

        // ...and the other device still works.
        var other = await _client.RefreshAsync(otherDevice.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
    }

    [Fact]
    public async Task Rotation_links_each_token_to_its_successor_within_one_family()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());
        var r1 = await _client.RefreshOkAsync(registered.RefreshToken);

        var (original, successor) = await api.QueryDbAsync(async db =>
        {
            var tokens = await db.RefreshTokens.Where(token => token.UserId == registered.User.Id).ToListAsync();
            return (tokens.Single(token => token.TokenHash == Hash(registered.RefreshToken)),
                    tokens.Single(token => token.TokenHash == Hash(r1.RefreshToken)));
        });

        Assert.NotNull(original.RevokedAt);
        Assert.Equal(successor.Id, original.ReplacedByTokenId);
        Assert.Equal(original.FamilyId, successor.FamilyId);
        Assert.Null(successor.RevokedAt);
    }

    [Fact]
    public async Task Two_parallel_refreshes_with_the_same_token_issue_exactly_one_successor()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var responses = await Task.WhenAll(
            _client.RefreshAsync(registered.RefreshToken),
            _client.RefreshAsync(registered.RefreshToken));

        // One wins. The other gets 401 — either because the xmin check failed its UPDATE, or
        // because it read the token after the winner committed and saw a reuse.
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);

        // Never two successors: the original plus exactly one new row.
        var rows = await api.QueryDbAsync(db => db.RefreshTokens.CountAsync(token => token.UserId == registered.User.Id));
        Assert.Equal(2, rows);
    }

    [Fact]
    public async Task An_unknown_token_returns_401()
    {
        var response = await _client.RefreshAsync("not-a-token-we-ever-issued");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_missing_token_returns_400()
    {
        var response = await _client.RefreshAsync("");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task The_token_of_a_deleted_user_returns_401()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());
        await api.QueryDbAsync(db => db.Users.Where(user => user.Id == registered.User.Id).ExecuteDeleteAsync());

        var response = await _client.RefreshAsync(registered.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task The_database_stores_no_raw_token_value()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());
        var r1 = await _client.RefreshOkAsync(registered.RefreshToken);

        var hashes = await api.QueryDbAsync(db => db.RefreshTokens
            .Where(token => token.UserId == registered.User.Id)
            .Select(token => token.TokenHash)
            .ToListAsync());

        Assert.DoesNotContain(registered.RefreshToken, hashes);
        Assert.DoesNotContain(r1.RefreshToken, hashes);
        Assert.Contains(Hash(registered.RefreshToken), hashes);
        Assert.Contains(Hash(r1.RefreshToken), hashes);
    }

    // Recomputed here rather than calling TokenService, so the test pins the stored format
    // (lowercase hex SHA-256 of the UTF-8 token) instead of trusting the code under test.
    private static string Hash(string refreshToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
