using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DevHub.Api.IntegrationTests.Harness;
using DevHub.Application.Auth.Contracts;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DevHub.Api.IntegrationTests.Auth;

/// <summary>DEVHUB-015's acceptance criteria: login, the access token, and credential failures.</summary>
public sealed class LoginTests(DevHubApiFactory api) : IClassFixture<DevHubApiFactory>
{
    private readonly HttpClient _client = api.CreateClient();

    [Fact]
    public async Task A_valid_login_returns_200_with_a_token_that_expires_in_15_minutes()
    {
        var email = AuthApi.UniqueEmail();
        var registered = await _client.RegisterOkAsync(email);

        var response = await _client.LoginAsync(email.ToUpperInvariant(), AuthApi.ValidPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal(registered.User.Id, body.User.Id);
        Assert.Equal(900, body.ExpiresIn);

        var token = new JsonWebToken(body.AccessToken);
        Assert.Equal(TimeSpan.FromMinutes(15), token.ValidTo - token.IssuedAt);
    }

    [Fact]
    public async Task The_access_token_carries_identity_only()
    {
        var email = AuthApi.UniqueEmail();
        var body = await _client.RegisterOkAsync(email);

        var token = new JsonWebToken(body.AccessToken);

        Assert.Equal(body.User.Id.ToString(), token.Subject);
        Assert.Equal(email, token.GetClaim("email").Value);
        Assert.Equal("Ada", token.GetClaim("name").Value);
        Assert.Equal("devhub-api", token.Issuer);
        Assert.Contains("devhub-clients", token.Audiences);
        Assert.False(string.IsNullOrEmpty(token.Id)); // jti
        Assert.Equal("HS256", token.Alg);

        // Roles live in the database, read per request (auth-spec.md §3).
        Assert.DoesNotContain(token.Claims, claim =>
            claim.Type.Contains("role", StringComparison.OrdinalIgnoreCase)
            || claim.Type.Contains("workspace", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task The_token_is_accepted_by_a_protected_endpoint()
    {
        var body = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var response = await GetWhoAmIAsync(body.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var whoAmI = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(body.User.Id.ToString(), whoAmI.GetProperty("sub").GetString());
    }

    [Fact]
    public async Task A_protected_endpoint_without_a_token_returns_401_problem_details()
    {
        var response = await GetWhoAmIAsync(accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// Expired by one second. With the default 5-minute ClockSkew this token would still be
    /// accepted; with ClockSkew = 0 it must not be.
    /// </summary>
    [Fact]
    public async Task An_expired_token_is_rejected()
    {
        var now = DateTime.UtcNow;
        var expired = ForgeToken(DevHubApiFactory.JwtSecret, issuedAt: now.AddMinutes(-15).AddSeconds(-1), expires: now.AddSeconds(-1));
        var stillValid = ForgeToken(DevHubApiFactory.JwtSecret, issuedAt: now, expires: now.AddMinutes(15));

        // The control: the forged token is otherwise well-formed, so the 401 below is about expiry.
        Assert.Equal(HttpStatusCode.OK, (await GetWhoAmIAsync(stillValid)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWhoAmIAsync(expired)).StatusCode);
    }

    [Fact]
    public async Task A_token_with_a_tampered_signature_is_rejected()
    {
        var body = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        // Flip the first character of the signature segment. (The last character is a poor
        // choice: some of its bits are base64url padding, so a change there can decode to the
        // same bytes and not tamper with anything.)
        var parts = body.AccessToken.Split('.');
        parts[2] = (parts[2][0] == 'A' ? 'B' : 'A') + parts[2][1..];

        var response = await GetWhoAmIAsync(string.Join('.', parts));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_signed_with_another_secret_is_rejected()
    {
        var now = DateTime.UtcNow;
        var forged = ForgeToken("an-attacker-guessed-secret-of-plenty-length", issuedAt: now, expires: now.AddMinutes(15));

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWhoAmIAsync(forged)).StatusCode);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_are_indistinguishable()
    {
        var email = AuthApi.UniqueEmail();
        await _client.RegisterOkAsync(email);

        var wrongPassword = await _client.LoginAsync(email, "not-the-right-password");
        var unknownEmail = await _client.LoginAsync(AuthApi.UniqueEmail("nobody"), "not-the-right-password");

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);

        var a = await wrongPassword.Content.ReadFromJsonAsync<JsonElement>();
        var b = await unknownEmail.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var field in new[] { "type", "title", "detail", "status" })
        {
            Assert.Equal(a.GetProperty(field).ToString(), b.GetProperty(field).ToString());
        }
    }

    /// <summary>
    /// Without the dummy verification an unknown email answers in about a millisecond and a
    /// known one in the ~100 ms PBKDF2 takes. The bound is deliberately loose (half) so a busy CI
    /// machine does not flake it; the difference it catches is two orders of magnitude.
    /// </summary>
    [Fact]
    public async Task An_unknown_email_takes_comparable_time_to_a_wrong_password()
    {
        const int samples = 5;

        // One attempt per account, so the per-account lockout never gets involved.
        var emails = new List<string>();
        for (var i = 0; i < samples; i++)
        {
            emails.Add(AuthApi.UniqueEmail("timing"));
            await _client.RegisterOkAsync(emails[^1]);
        }

        await _client.LoginAsync(AuthApi.UniqueEmail("warmup"), "not-the-right-password");

        var wrongPassword = new List<TimeSpan>();
        var unknownEmail = new List<TimeSpan>();
        foreach (var email in emails)
        {
            wrongPassword.Add(await TimeAsync(() => _client.LoginAsync(email, "not-the-right-password")));
            unknownEmail.Add(await TimeAsync(() => _client.LoginAsync(AuthApi.UniqueEmail("nobody"), "not-the-right-password")));
        }

        var wrongMedian = Median(wrongPassword);
        var unknownMedian = Median(unknownEmail);

        Assert.True(
            unknownMedian >= wrongMedian / 2,
            $"Unknown email median {unknownMedian.TotalMilliseconds:F0} ms vs wrong password {wrongMedian.TotalMilliseconds:F0} ms.");
    }

    [Fact]
    public async Task Missing_credentials_return_400_not_401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpResponseMessage> GetWhoAmIAsync(string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/whoami");
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await _client.SendAsync(request);
    }

    private static string ForgeToken(string secret, DateTime issuedAt, DateTime expires) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "devhub-api",
            Audience = "devhub-clients",
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expires,
            Claims = new Dictionary<string, object> { ["sub"] = Guid.NewGuid().ToString() },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret)),
                SecurityAlgorithms.HmacSha256),
        });

    private static async Task<TimeSpan> TimeAsync(Func<Task<HttpResponseMessage>> request)
    {
        var stopwatch = Stopwatch.StartNew();
        using var response = await request();
        stopwatch.Stop();
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        return stopwatch.Elapsed;
    }

    private static TimeSpan Median(List<TimeSpan> values) => values.Order().ElementAt(values.Count / 2);
}
