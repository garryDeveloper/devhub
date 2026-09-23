using System.Net.Http.Json;
using DevHub.Application.Auth.Contracts;

namespace DevHub.Api.IntegrationTests.Harness;

/// <summary>Small helpers over the auth endpoints, so each test reads as its scenario.</summary>
public static class AuthApi
{
    public const string ValidPassword = "correct-horse-battery";

    public static string UniqueEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@devhub.test";

    public static Task<HttpResponseMessage> RegisterAsync(
        this HttpClient client, string email, string password = ValidPassword, string displayName = "Ada") =>
        client.PostAsJsonAsync("/api/auth/register", new { email, password, displayName });

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    public static async Task<AuthResponse> RegisterOkAsync(this HttpClient client, string email)
    {
        var response = await client.RegisterAsync(email);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public static Task<HttpResponseMessage> RefreshAsync(this HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

    public static async Task<RefreshResponse> RefreshOkAsync(this HttpClient client, string refreshToken)
    {
        var response = await client.RefreshAsync(refreshToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RefreshResponse>())!;
    }

    public static Task<HttpResponseMessage> LogoutAsync(this HttpClient client, string? refreshToken) =>
        client.PostAsJsonAsync("/api/auth/logout", new { refreshToken });
}
