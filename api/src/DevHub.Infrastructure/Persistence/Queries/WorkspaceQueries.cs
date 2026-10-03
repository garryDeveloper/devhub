using DevHub.Application.Users.Contracts;
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

    public async Task<IReadOnlyList<MemberDto>?> ListMembersAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        // The membership check and the list come from the same table, but as two queries rather
        // than one: EF cannot express "this row exists, and also give me every row" as a single
        // round trip without reading the target workspace's members twice over the wire anyway.
        var isMember = await dbContext.Set<WorkspaceMember>()
            .AsNoTracking()
            .AnyAsync(member => member.WorkspaceId == workspaceId && member.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (!isMember)
        {
            return null;
        }

        var rows = await dbContext.Set<WorkspaceMember>()
            .AsNoTracking()
            .Where(member => member.WorkspaceId == workspaceId)
            .Join(
                dbContext.Users,
                member => member.UserId,
                user => user.Id,
                (member, user) => new MemberRow
                {
                    Id = member.Id,
                    Role = member.Role,
                    JoinedAt = member.JoinedAt,
                    UserId = user.Id,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                })
            .OrderBy(row => row.JoinedAt)
            .ThenBy(row => row.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. rows.Select(ToMemberDto)];
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

    private static MemberDto ToMemberDto(MemberRow row) =>
        new(row.Id, new UserDto(row.UserId, row.Email, row.DisplayName, AvatarUrl: null), row.Role.ToString(), row.JoinedAt);

    private sealed class WorkspaceRow
    {
        public required Guid Id { get; init; }

        public required string Name { get; init; }

        public required string Slug { get; init; }

        public required WorkspaceRole Role { get; init; }

        public required int MemberCount { get; init; }

        public required DateTimeOffset CreatedAt { get; init; }
    }

    private sealed class MemberRow
    {
        public required Guid Id { get; init; }

        public required WorkspaceRole Role { get; init; }

        public required DateTimeOffset JoinedAt { get; init; }

        public required Guid UserId { get; init; }

        public required string Email { get; init; }

        public required string DisplayName { get; init; }
    }
}
