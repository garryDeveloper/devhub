namespace DevHub.Application.Projects.Get;

/// <summary><c>GET /api/projects/{projectId}</c>.</summary>
public sealed record GetProjectQuery(Guid ProjectId);
