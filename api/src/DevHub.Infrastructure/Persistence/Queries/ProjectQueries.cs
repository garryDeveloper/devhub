using DevHub.Application.Projects;
using DevHub.Application.Projects.Contracts;
using DevHub.Domain.Projects;
using DevHub.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace DevHub.Infrastructure.Persistence.Queries;

/// <summary>
/// <see cref="IProjectQueries"/> as single projected SQL queries: no aggregate is materialized and
/// nothing is tracked. The membership filter is part of the query itself, so a project the caller
/// cannot see never leaves the database.
/// </summary>
internal sealed class ProjectQueries(DevHubDbContext dbContext) : IProjectQueries
{
    public async Task<IReadOnlyList<ProjectSummaryDto>?> ListForWorkspaceAsync(
        Guid workspaceId, Guid userId, bool includeArchived, CancellationToken cancellationToken)
    {
        // Same two-query shape as WorkspaceQueries.ListMembersAsync: membership is on a different
        // table than the rows being listed, so it cannot be folded into one round trip without
        // re-reading the projects over the wire for every member row.
        var isMember = await dbContext.Set<WorkspaceMember>()
            .AsNoTracking()
            .AnyAsync(member => member.WorkspaceId == workspaceId && member.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (!isMember)
        {
            return null;
        }

        var rows = await dbContext.Set<Project>()
            .AsNoTracking()
            .Where(project => project.WorkspaceId == workspaceId)
            .Where(project => includeArchived || project.ArchivedAt == null)
            .OrderBy(project => project.Name)
            .ThenBy(project => project.Id)
            .Select(project => new ProjectSummaryDto(
                project.Id,
                project.Name,
                project.Key,
                project.Description,
                project.Color,
                project.Icon,
                // TODO(EPIC 5 / Issues): a subquery over issues once that entity exists
                // (DEVHUB-031 technical notes) — never a loaded collection.
                0,
                project.ArchivedAt,
                project.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows;
    }

    public async Task<ProjectDto?> GetForUserAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        var dto = await dbContext.Set<Project>()
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .Join(
                dbContext.Set<WorkspaceMember>().Where(member => member.UserId == userId),
                project => project.WorkspaceId,
                member => member.WorkspaceId,
                (project, _) => new ProjectDto(
                    project.Id,
                    project.Name,
                    project.Key,
                    project.Description,
                    project.Color,
                    project.Icon))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return dto;
    }
}
