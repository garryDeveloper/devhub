namespace DevHub.Application.Workspaces.List;

/// <summary>
/// <c>GET /api/workspaces</c>. No parameters and no paging: the list is always the caller's own
/// memberships, and a user belongs to a handful of workspaces, not thousands.
/// </summary>
public sealed record ListWorkspacesQuery;
