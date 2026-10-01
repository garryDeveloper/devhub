using DevHub.Domain.Workspaces;

namespace DevHub.Application.Workspaces;

/// <summary>
/// Loads and stages <see cref="Workspace"/> aggregates for commands. Staging only — nothing is
/// written until the handler calls <see cref="Common.IUnitOfWork.SaveChangesAsync"/>.
/// Reads that only return data go through <see cref="IWorkspaceQueries"/> instead.
/// </summary>
public interface IWorkspaceRepository
{
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);

    /// <summary>The whole aggregate, members included: every rule on it is a rule over them.</summary>
    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Workspace workspace);
}
