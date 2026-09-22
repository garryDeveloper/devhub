namespace DevHub.Application.Auth.Register;

/// <summary><c>POST /api/auth/register</c>. The password is plaintext here and nowhere after the handler.</summary>
public sealed record RegisterCommand(string Email, string Password, string DisplayName);
