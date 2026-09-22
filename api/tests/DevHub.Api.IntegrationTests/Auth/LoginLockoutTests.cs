using System.Net;
using DevHub.Api.IntegrationTests.Harness;

namespace DevHub.Api.IntegrationTests.Auth;

/// <summary>
/// Per-account lockout: 5 failures in 15 minutes → 429 with Retry-After (auth-spec.md §6).
/// </summary>
/// <remarks>
/// Its own class, so its own factory and clock: moving the clock forward here must not leave
/// other tests issuing tokens that are "not valid yet".
/// </remarks>
public sealed class LoginLockoutTests(DevHubApiFactory api) : IClassFixture<DevHubApiFactory>
{
    private readonly HttpClient _client = api.CreateClient();

    [Fact]
    public async Task Five_failures_lock_the_account_even_for_the_right_password_until_the_window_passes()
    {
        var email = AuthApi.UniqueEmail();
        await _client.RegisterOkAsync(email);

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var failure = await _client.LoginAsync(email, "wrong-password-guess");
            Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
        }

        var locked = await _client.LoginAsync(email, AuthApi.ValidPassword);

        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
        var retryAfter = locked.Headers.RetryAfter?.Delta;
        Assert.NotNull(retryAfter);
        Assert.InRange(retryAfter.Value, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));

        api.Clock.Advance(TimeSpan.FromMinutes(15));

        var afterWindow = await _client.LoginAsync(email, AuthApi.ValidPassword);
        Assert.Equal(HttpStatusCode.OK, afterWindow.StatusCode);
    }

    /// <summary>
    /// Locking only real accounts would turn "429 vs 401" into an account-existence oracle.
    /// </summary>
    [Fact]
    public async Task Unknown_emails_lock_out_exactly_like_real_ones()
    {
        var email = AuthApi.UniqueEmail("nobody");

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.LoginAsync(email, "guess")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await _client.LoginAsync(email, "guess")).StatusCode);
    }

    [Fact]
    public async Task A_successful_login_resets_the_failure_count()
    {
        var email = AuthApi.UniqueEmail();
        await _client.RegisterOkAsync(email);

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            await _client.LoginAsync(email, "wrong-password-guess");
        }

        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(email, AuthApi.ValidPassword)).StatusCode);

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.LoginAsync(email, "wrong-password-guess")).StatusCode);
        }
    }
}
