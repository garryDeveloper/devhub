using System.Net;
using DevHub.Api.IntegrationTests.Harness;

namespace DevHub.Api.IntegrationTests.Auth;

/// <summary>The per-IP limit on the auth endpoints, with a small limit so the test stays fast.</summary>
public sealed class AuthRateLimitTests(AuthRateLimitTests.LimitedApiFactory api)
    : IClassFixture<AuthRateLimitTests.LimitedApiFactory>
{
    private const int Limit = 3;

    [Fact]
    public async Task Requests_over_the_limit_get_429_problem_details_with_Retry_After()
    {
        var client = api.CreateClient();

        for (var i = 0; i < Limit; i++)
        {
            var allowed = await client.LoginAsync(AuthApi.UniqueEmail(), "whatever-password");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
        }

        // Register shares the budget with login: one bucket per IP across both endpoints.
        var rejected = await client.RegisterAsync(AuthApi.UniqueEmail());

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(rejected.Headers.RetryAfter);
    }

    public sealed class LimitedApiFactory : DevHubApiFactory
    {
        protected override int AuthPermitLimit => Limit;
    }
}
