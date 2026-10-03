namespace DevHub.Application.Workspaces.Members.Remove;

/// <summary>
/// <c>DELETE /api/workspaces/{workspaceId}/members/{memberId}</c>. Owner only, except a member
/// removing themselves (leaving the workspace) — that needs no role at all.
/// </summary>
public sealed record RemoveMemberCommand(Guid WorkspaceId, Guid MemberId);
