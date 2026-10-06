using DevHub.Application.Projects.Contracts;

namespace DevHub.Application.Projects;

/// <summary>
/// The read side of projects: projections straight to their DTOs, no aggregate loaded, nothing
/// tracked — same split as <see cref="Workspaces.IWorkspaceQueries"/>.
/// </summary>
/// <remarks>
/// Both methods are scoped to <c>userId</c> inside the query: there is no overload that returns a
/// project without asking who is looking. Null means "does not exist" and "exists, but the caller
/// is not a member of its workspace" at once — both become the same 404 (DEVHUB-031).
/// </remarks>
public interface IProjectQueries
{
    /// <summary>
    /// Every project in <paramref name="workspaceId"/>, archived ones included only when
    /// <paramref name="includeArchived"/> is true. Null if <paramref name="workspaceId"/> does not
    /// exist or <paramref name="userId"/> is not a member of it.
    /// </summary>
    Task<IReadOnlyList<ProjectSummaryDto>?> ListForWorkspaceAsync(
        Guid workspaceId, Guid userId, bool includeArchived, CancellationToken cancellationToken);

    /// <summary>
    /// Null when the project does not exist, or <paramref name="userId"/> is not a member of its
    /// workspace. Every workspace member may read every project (domain-model.md) — membership of
    /// the project itself is never checked here.
    /// </summary>
    Task<ProjectDto?> GetForUserAsync(Guid projectId, Guid userId, CancellationToken cancellationToken);
}
