namespace DevHub.Application.Projects.List;

/// <summary><c>GET /api/workspaces/{workspaceId}/projects?includeArchived=false</c>.</summary>
public sealed record ListProjectsQuery(Guid WorkspaceId, bool IncludeArchived);
