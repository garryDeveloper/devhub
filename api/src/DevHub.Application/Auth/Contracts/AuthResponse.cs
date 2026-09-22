using DevHub.Application.Users.Contracts;

namespace DevHub.Application.Auth.Contracts;

/// <summary>
/// What register (201) and login (200) both return (api-endpoints.md §1).
/// </summary>
/// <param name="ExpiresIn">Access token lifetime in seconds, so clients need not decode the JWT.</param>
public sealed record AuthResponse(string AccessToken, int ExpiresIn, string RefreshToken, UserDto User);
