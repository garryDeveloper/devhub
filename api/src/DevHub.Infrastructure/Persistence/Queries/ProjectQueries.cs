using DevHub.Application.Projects;
using DevHub.Application.Projects.Contracts;
using DevHub.Application.Users.Contracts;
using DevHub.Application.Workspaces.Contracts;
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

    public async Task<IReadOnlyList<ProjectMemberDto>?> ListMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        // Visibility is workspace membership, not project membership (domain-model.md,
        // DEVHUB-032): project membership only narrows who can be assigned issues, it never
        // narrows who can read the project — same rule GetForUserAsync applies above.
        var workspaceId = await dbContext.Set<Project>()
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .Select(project => (Guid?)project.WorkspaceId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (workspaceId is null)
        {
            return null;
        }

        var isWorkspaceMember = await dbContext.Set<WorkspaceMember>()
            .AsNoTracking()
            .AnyAsync(member => member.WorkspaceId == workspaceId && member.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (!isWorkspaceMember)
        {
            return null;
        }

        var rows = await dbContext.Set<ProjectMember>()
            .AsNoTracking()
            .Where(member => member.ProjectId == projectId)
            .Join(
                dbContext.Users,
                member => member.UserId,
                user => user.Id,
                (member, user) => new MemberRow
                {
                    Id = member.Id,
                    Role = member.Role,
                    AddedAt = member.AddedAt,
                    UserId = user.Id,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                })
            .OrderBy(row => row.AddedAt)
            .ThenBy(row => row.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. rows.Select(ToMemberDto)];
    }

    private static ProjectMemberDto ToMemberDto(MemberRow row) =>
        new(row.Id, new UserDto(row.UserId, row.Email, row.DisplayName, AvatarUrl: null), row.Role.ToString(), row.AddedAt);

    private sealed class MemberRow
    {
        public required Guid Id { get; init; }

        public required ProjectRole Role { get; init; }

        public required DateTimeOffset AddedAt { get; init; }

        public required Guid UserId { get; init; }

        public required string Email { get; init; }

        public required string DisplayName { get; init; }
    }
}
