namespace DevHub.Application.Auth.Logout;

/// <summary>
/// <c>POST /api/auth/logout</c>. <see cref="RefreshToken"/> is nullable on purpose: a missing
/// token is not an error here, it just means there is nothing to revoke (DEVHUB-017).
/// </summary>
public sealed record LogoutCommand(string? RefreshToken);
