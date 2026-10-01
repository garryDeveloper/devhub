using DevHub.Application.Workspaces;
using DevHub.Application.Workspaces.Contracts;
using DevHub.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace DevHub.Infrastructure.Persistence.Queries;

/// <summary>
/// <see cref="IWorkspaceQueries"/> as single projected SQL queries: no aggregate is materialized
/// and nothing is tracked. The membership filter is part of the query itself, so a workspace the
/// caller does not belong to never leaves the database.
/// </summary>
internal sealed class WorkspaceQueries(DevHubDbContext dbContext) : IWorkspaceQueries
{
    public async Task<IReadOnlyList<WorkspaceDto>> ListForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await ForUser(userId)
            .OrderBy(row => row.Name)
            .ThenBy(row => row.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. rows.Select(ToDto)];
    }

    public async Task<WorkspaceDto?> GetForUserAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var row = await ForUser(userId)
            .SingleOrDefaultAsync(row => row.Id == workspaceId, cancellationToken)
            .ConfigureAwait(false);

        return row is null ? null : ToDto(row);
    }

    /// <summary>
    /// One row per workspace the user is a member of, carrying <b>their</b> role. Starts from the
    /// membership — served by <c>ix_workspace_members_user_id</c> — and joins to the workspace.
    /// </summary>
    private IQueryable<WorkspaceRow> ForUser(Guid userId) =>
        dbContext.Workspaces
            .AsNoTracking()
            .SelectMany(
                workspace => workspace.Members.Where(member => member.UserId == userId),
                // An object initializer, not a constructor call: EF can see which column feeds
                // which property, so the OrderBy/Where applied afterwards still translate to SQL.
                (workspace, member) => new WorkspaceRow
                {
                    Id = workspace.Id,
                    Name = workspace.Name,
                    Slug = workspace.Slug,
                    Role = member.Role,
                    MemberCount = workspace.Members.Count,
                    CreatedAt = workspace.CreatedAt,
                });

    // The role leaves SQL as the enum and becomes the wire string here, in memory: the text column
    // already holds "Owner", but relying on EF to translate Enum.ToString() is provider-specific.
    private static WorkspaceDto ToDto(WorkspaceRow row) =>
        new(row.Id, row.Name, row.Slug, row.Role.ToString(), row.MemberCount, row.CreatedAt);

    private sealed class WorkspaceRow
    {
        public required Guid Id { get; init; }

        public required string Name { get; init; }

        public required string Slug { get; init; }

        public required WorkspaceRole Role { get; init; }

        public required int MemberCount { get; init; }

        public required DateTimeOffset CreatedAt { get; init; }
    }
}
