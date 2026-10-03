namespace DevHub.Application.Workspaces.Members.Add;

/// <summary><c>POST /api/workspaces/{workspaceId}/members</c>. Owner only.</summary>
public sealed record AddMemberCommand(Guid WorkspaceId, string? Email, string? Role);
