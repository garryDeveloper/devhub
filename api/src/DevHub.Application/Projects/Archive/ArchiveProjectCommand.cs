namespace DevHub.Application.Projects.Archive;

/// <summary><c>DELETE /api/projects/{projectId}</c>. Not a row delete: it sets <c>archivedAt</c>
/// (DEVHUB-031 — permanent deletion is out of scope for the MVP).</summary>
public sealed record ArchiveProjectCommand(Guid ProjectId);
