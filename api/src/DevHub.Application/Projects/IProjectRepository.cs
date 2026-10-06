using DevHub.Domain.Projects;

namespace DevHub.Application.Projects;

/// <summary>
/// Loads and stages <see cref="Project"/> aggregates for commands. Staging only — nothing is
/// written until the handler calls <see cref="Common.IUnitOfWork.SaveChangesAsync"/>.
/// Reads that only return data go through <see cref="IProjectQueries"/> instead.
/// </summary>
public interface IProjectRepository
{
    Task<bool> KeyExistAsync(string key, Guid workspaceId, CancellationToken cancellationToken);

    /// <summary>The whole aggregate, members included: same shape as
    /// <see cref="Workspaces.IWorkspaceRepository.GetByIdAsync"/>, for the handlers DEVHUB-032 adds.</summary>
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Project project);
}
