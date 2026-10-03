namespace DevHub.Application.Workspaces.Members.ChangeRole;

/// <summary><c>PATCH /api/workspaces/{workspaceId}/members/{memberId}</c>. Owner only.</summary>
public sealed record ChangeMemberRoleCommand(Guid WorkspaceId, Guid MemberId, string? Role);
