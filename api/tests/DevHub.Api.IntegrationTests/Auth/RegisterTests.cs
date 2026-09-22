using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DevHub.Api.IntegrationTests.Harness;
using DevHub.Application.Auth.Contracts;
using DevHub.Application.Common;
using DevHub.Application.Users;
using DevHub.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevHub.Api.IntegrationTests.Auth;

/// <summary>DEVHUB-014's acceptance criteria, over real HTTP and a real PostgreSQL.</summary>
public sealed class RegisterTests(DevHubApiFactory api) : IClassFixture<DevHubApiFactory>
{
    private readonly HttpClient _client = api.CreateClient();

    [Fact]
    public async Task A_new_email_returns_201_with_tokens_and_the_user()
    {
        var email = AuthApi.UniqueEmail("ada");

        var response = await _client.RegisterAsync($"  {email.ToUpperInvariant()}  ", displayName: "  Ada Lovelace ");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.Equal(900, body.ExpiresIn);

        // Stored the way User.Create normalizes it, and trimmed.
        Assert.Equal(email, body.User.Email);
        Assert.Equal("Ada Lovelace", body.User.DisplayName);
        Assert.Null(body.User.AvatarUrl);
        Assert.NotEqual(Guid.Empty, body.User.Id);
    }

    [Fact]
    public async Task The_response_never_contains_the_password_hash()
    {
        var response = await _client.RegisterAsync(AuthApi.UniqueEmail());

        var json = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(AuthApi.ValidPassword, json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Registration_persists_the_user_and_only_the_hash_of_the_refresh_token()
    {
        var body = await _client.RegisterOkAsync(AuthApi.UniqueEmail());

        var stored = await api.QueryDbAsync(db => db.RefreshTokens.SingleAsync(t => t.UserId == body.User.Id));
        var user = await api.QueryDbAsync(db => db.Users.SingleAsync(u => u.Id == body.User.Id));

        Assert.Equal(Sha256Hex(body.RefreshToken), stored.TokenHash);
        Assert.NotEqual(body.RefreshToken, stored.TokenHash);
        Assert.NotEqual(AuthApi.ValidPassword, user.PasswordHash);
    }

    [Fact]
    public async Task A_duplicate_email_returns_409_and_creates_nothing()
    {
        var email = AuthApi.UniqueEmail();
        var first = await _client.RegisterOkAsync(email);

        // Different casing and whitespace: still the same account (citext + normalization).
        var response = await _client.RegisterAsync($" {email.ToUpperInvariant()} ", displayName: "Impostor");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, problem.GetProperty("status").GetInt32());
        Assert.Equal("https://devhub.dev/errors/auth.email_taken", problem.GetProperty("type").GetString());

        Assert.Equal(1, await api.QueryDbAsync(db => db.Users.CountAsync(u => u.Email == email)));
        Assert.Equal(1, await api.QueryDbAsync(db => db.RefreshTokens.CountAsync(t => t.UserId == first.User.Id)));
    }

    /// <summary>
    /// The ticket's race condition: concurrent registrations of one email yield exactly one
    /// user. Some requests are stopped by the pre-check, the rest by the unique index — the
    /// assertion holds whichever path each one took.
    /// </summary>
    [Fact]
    public async Task Concurrent_registrations_of_the_same_email_create_exactly_one_user()
    {
        var email = AuthApi.UniqueEmail("race");

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => _client.RegisterAsync(email)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(
            responses.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        Assert.Equal(1, await api.QueryDbAsync(db => db.Users.CountAsync(u => u.Email == email)));
    }

    /// <summary>
    /// The half of the race the test above cannot force: the pre-check said "free" but the
    /// index says otherwise. The unit of work must surface that as the Application's own
    /// exception — the one RegisterHandler turns into 409 — not as a raw Npgsql error (500).
    /// </summary>
    [Fact]
    public async Task A_unique_index_violation_surfaces_as_UniqueConstraintViolationException()
    {
        var email = AuthApi.UniqueEmail();
        await _client.RegisterOkAsync(email);

        await using var scope = api.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IUserRepository>().Add(User.Create(email, "Second", "hash"));

        var exception = await Assert.ThrowsAsync<UniqueConstraintViolationException>(
            () => scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None));

        Assert.Equal("ix_users_email", exception.ConstraintName);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("123456789")] // 9 characters: one short of the minimum
    [InlineData("password123")] // long enough, but on the common-password list
    public async Task A_weak_password_returns_400_with_a_password_error(string password)
    {
        var email = AuthApi.UniqueEmail();

        var response = await _client.RegisterAsync(email, password);

        var errors = await AssertValidationProblemAsync(response);
        Assert.True(errors.TryGetProperty("password", out _), $"Expected errors.password, got {errors}");
        Assert.Equal(0, await api.QueryDbAsync(db => db.Users.CountAsync(u => u.Email == email)));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@devhub.test")]
    [InlineData("")]
    public async Task An_invalid_email_returns_400_with_an_email_error(string email)
    {
        var response = await _client.RegisterAsync(email);

        var errors = await AssertValidationProblemAsync(response);
        Assert.True(errors.TryGetProperty("email", out _), $"Expected errors.email, got {errors}");
    }

    [Fact]
    public async Task Missing_fields_are_reported_by_the_validator_with_camelCase_keys()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { });

        var errors = await AssertValidationProblemAsync(response);
        Assert.True(errors.TryGetProperty("email", out _));
        Assert.True(errors.TryGetProperty("password", out _));
        Assert.True(errors.TryGetProperty("displayName", out _));
    }

    private static async Task<JsonElement> AssertValidationProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://devhub.dev/errors/validation", problem.GetProperty("type").GetString());
        Assert.True(problem.TryGetProperty("traceId", out _), "Every ProblemDetails carries a traceId.");
        return problem.GetProperty("errors");
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
