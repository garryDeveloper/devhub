namespace DevHub.Application.Auth.Login;

/// <summary><c>POST /api/auth/login</c>.</summary>
public sealed record LoginCommand(string Email, string Password);
