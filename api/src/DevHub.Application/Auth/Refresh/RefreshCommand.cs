namespace DevHub.Application.Auth.Refresh;

/// <summary><c>POST /api/auth/refresh</c>.</summary>
public sealed record RefreshCommand(string RefreshToken);
