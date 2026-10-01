namespace DevHub.Application.Workspaces.Get;

/// <summary><c>GET /api/workspaces/{workspaceId}</c>.</summary>
public sealed record GetWorkspaceQuery(Guid WorkspaceId);
