namespace DevHub.Application.Auth.Contracts;

/// <summary>
/// What <c>POST /api/auth/refresh</c> returns (api-endpoints.md §1): a new pair, no user. The
/// client already has the user from login, and refreshing is not the moment to re-read it.
/// </summary>
/// <param name="ExpiresIn">Access token lifetime in seconds, as in <see cref="AuthResponse"/>.</param>
public sealed record RefreshResponse(string AccessToken, int ExpiresIn, string RefreshToken);
