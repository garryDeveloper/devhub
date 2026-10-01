namespace DevHub.Application.Workspaces.Create;

/// <summary><c>POST /api/workspaces</c>. The owner is always the caller, never a field in the body.</summary>
/// <param name="Slug">Optional. Absent or null derives it from <paramref name="Name"/>; present is used as-is.</param>
public sealed record CreateWorkspaceCommand(string? Name, string? Slug);
