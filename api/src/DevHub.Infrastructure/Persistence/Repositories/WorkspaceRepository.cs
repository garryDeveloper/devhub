using DevHub.Application.Workspaces;
using DevHub.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace DevHub.Infrastructure.Persistence.Repositories;

internal sealed class WorkspaceRepository(DevHubDbContext dbContext) : IWorkspaceRepository
{
    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.Workspaces.AnyAsync(workspace => workspace.Slug == slug, cancellationToken);

    public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Workspaces
            .Include(workspace => workspace.Members)
            .SingleOrDefaultAsync(workspace => workspace.Id == id, cancellationToken);

    public void Add(Workspace workspace) => dbContext.Workspaces.Add(workspace);
}
