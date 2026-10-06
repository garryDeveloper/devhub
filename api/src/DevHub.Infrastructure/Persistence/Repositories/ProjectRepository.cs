using DevHub.Application.Projects;
using DevHub.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace DevHub.Infrastructure.Persistence.Repositories;

internal sealed class ProjectRepository(DevHubDbContext dbContext) : IProjectRepository
{
    public void Add(Project project) => dbContext.Projects.Add(project);

    public Task<bool> KeyExistAsync(string key, Guid workspaceId, CancellationToken cancellationToken) =>
        dbContext.Projects.AnyAsync(project => project.Key == key && project.WorkspaceId == workspaceId, cancellationToken);

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Projects
            .Include(project => project.Members)
            .SingleOrDefaultAsync(project => project.Id == id, cancellationToken);
}
