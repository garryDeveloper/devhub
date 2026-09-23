using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DevHub.Api.IntegrationTests.Harness;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace DevHub.Api.IntegrationTests.Auth;

/// <summary>DEVHUB-018: protected by default, public only on purpose (auth-spec.md §6).</summary>
public sealed class EndpointProtectionTests(DevHubApiFactory api) : IClassFixture<DevHubApiFactory>
{
    /// <summary>
    /// Every endpoint reachable without a token. Adding one to the API fails
    /// <see cref="Anonymous_endpoints_are_exactly_the_allow_list"/> until it is added here too — on
    /// purpose, so making something public is a decision someone reviews, never a side effect of
    /// mapping a route inside an anonymous group.
    /// </summary>
    private static readonly string[] AnonymousAllowList =
    [
        "* /health",
        "POST /api/auth/login",
        "POST /api/auth/logout",
        "POST /api/auth/refresh",
        "POST /api/auth/register",
    ];

    private readonly HttpClient _client = api.CreateClient();

    [Fact]
    public void Every_endpoint_is_explicitly_anonymous_or_requires_authorization()
    {
        var unmarked = MappedEndpoints()
            .Where(endpoint => Classify(endpoint) == Protection.Unmarked)
            .Select(Describe)
            .ToList();

        Assert.Empty(unmarked);
    }

    [Fact]
    public void Anonymous_endpoints_are_exactly_the_allow_list()
    {
        var anonymous = MappedEndpoints()
            .Where(endpoint => Classify(endpoint) == Protection.Anonymous)
            .Select(Describe)
            .Order(StringComparer.Ordinal);

        Assert.Equal(AnonymousAllowList.Order(StringComparer.Ordinal), anonymous);
    }

    /// <summary>
    /// The enumeration test is only worth something if it can fail: an endpoint mapped with no
    /// authorization metadata at all must be reported.
    /// </summary>
    [Fact]
    public void An_endpoint_with_neither_marking_is_detected()
    {
        var bare = new RouteEndpoint(
            _ => Task.CompletedTask, RoutePatternFactory.Parse("/api/forgotten"), 0, EndpointMetadataCollection.Empty, "bare");

        Assert.Equal(Protection.Unmarked, Classify(bare));
    }

    [Fact]
    public async Task A_protected_endpoint_without_a_token_returns_401_problem_details()
    {
        var response = await _client.GetAsync("/api/test/whoami");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
        var problem = await AssertProblemAsync(response);
        Assert.Equal("https://devhub.dev/errors/auth.unauthenticated", problem.GetProperty("type").GetString());
    }

    [Fact]
    public async Task An_invalid_token_returns_the_same_401_problem_details()
    {
        var response = await SendAsync("/api/test/whoami", accessToken: "not-a-jwt");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await AssertProblemAsync(response);
        Assert.Equal("https://devhub.dev/errors/auth.unauthenticated", problem.GetProperty("type").GetString());
    }

    [Fact]
    public async Task A_failed_policy_returns_403_problem_details()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var response = await SendAsync("/api/test/forbidden", registered.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await AssertProblemAsync(response);
        Assert.Equal("https://devhub.dev/errors/auth.forbidden", problem.GetProperty("type").GetString());
    }

    [Fact]
    public async Task An_endpoints_own_401_keeps_its_specific_type()
    {
        var response = await _client.RefreshAsync("not-a-token-we-ever-issued");

        var problem = await AssertProblemAsync(response);
        Assert.Equal("https://devhub.dev/errors/auth.invalid_refresh_token", problem.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Health_works_anonymously()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_works_anonymously()
    {
        var email = AuthApi.UniqueEmail();
        await _client.RegisterOkAsync(email);

        var response = await _client.LoginAsync(email, AuthApi.ValidPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// The fallback policy also runs when no endpoint matched: without a token, an unknown route
    /// is indistinguishable from a protected one. With a token, it is an honest 404.
    /// </summary>
    [Fact]
    public async Task An_unknown_route_is_401_anonymously_and_404_when_authenticated()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var anonymous = await _client.GetAsync("/api/does-not-exist");
        var authenticated = await SendAsync("/api/does-not-exist", registered.AccessToken);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, authenticated.StatusCode);
    }

    [Fact]
    public async Task CurrentUser_is_populated_from_the_access_token()
    {
        var registered = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var response = await SendAsync("/api/test/current-user", registered.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(registered.User.Id, body.GetProperty("userId").GetGuid());
        Assert.True(body.GetProperty("isAuthenticated").GetBoolean());
    }

    private enum Protection
    {
        Unmarked,
        Anonymous,
        RequiresAuthorization,
    }

    /// <summary>
    /// Mirrors how the authorization middleware reads endpoint metadata: <see cref="IAllowAnonymous"/>
    /// wins over any <see cref="IAuthorizeData"/> (so a route inside the anonymous /auth group is
    /// anonymous despite the /api group's RequireAuthorization()).
    /// </summary>
    private static Protection Classify(Endpoint endpoint)
    {
        var metadata = endpoint.Metadata;

        if (metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return Protection.Anonymous;
        }

        return metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0
            || metadata.GetMetadata<AuthorizationPolicy>() is not null
                ? Protection.RequiresAuthorization
                : Protection.Unmarked;
    }

    private IEnumerable<RouteEndpoint> MappedEndpoints() =>
        api.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();

    private static string Describe(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
        var verb = methods is { Count: > 0 } ? string.Join(",", methods) : "*";
        return $"{verb} /{endpoint.RoutePattern.RawText?.TrimStart('/')}";
    }

    private async Task<HttpResponseMessage> SendAsync(string path, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.TryGetProperty("traceId", out _), "Every ProblemDetails carries a traceId.");
        return problem;
    }
}
