using System.Net;
using DevHub.Api.IntegrationTests.Harness;
using DevHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevHub.Api.IntegrationTests.Auth;

/// <summary>DEVHUB-016: refresh token expiry and the 60-day purge.</summary>
/// <remarks>
/// Its own class, so its own factory and clock: these tests move the clock by weeks, which would
/// make access tokens issued by other test classes "not valid yet".
/// </remarks>
public sealed class RefreshTokenLifetimeTests(DevHubApiFactory api) : IClassFixture<DevHubApiFactory>
{
    private readonly HttpClient _client = api.CreateClient();

    [Fact]
    public async Task An_expired_token_returns_401()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        api.Clock.Advance(TimeSpan.FromDays(30) + TimeSpan.FromSeconds(1));

        var response = await _client.RefreshAsync(registered.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task The_purge_deletes_tokens_expired_over_60_days_ago_and_keeps_the_rest()
    {
        var old = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        // `old` expires 30 days from now; move 29 days, then issue `recent`, which expires 30
        // days after that. Then land between the two cutoffs: `old` expired 61 days ago,
        // `recent` only 32.
        api.Clock.Advance(TimeSpan.FromDays(29));
        var recent = await _client.RegisterOkAsync(AuthApi.UniqueEmail());
        api.Clock.Advance(TimeSpan.FromDays(62));

        await api.Services.GetRequiredService<RefreshTokenCleanupService>().PurgeOnceAsync(CancellationToken.None);

        var remaining = await api.QueryDbAsync(db => db.RefreshTokens
            .Where(token => token.UserId == old.User.Id || token.UserId == recent.User.Id)
            .Select(token => token.UserId)
            .ToListAsync());
        Assert.Equal([recent.User.Id], remaining);

        // A purged token is simply unknown.
        var response = await _client.RefreshAsync(old.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
