namespace DevHub.Application.Workspaces.Members.List;

/// <summary><c>GET /api/workspaces/{workspaceId}/members</c>. Any member may call this.</summary>
public sealed record ListMembersQuery(Guid WorkspaceId);
