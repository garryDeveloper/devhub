using DevHub.Domain.Common;

namespace DevHub.Domain.Workspaces;

/// <summary>
/// A user's membership of one workspace, and the role that comes with it.
/// </summary>
/// <remarks>
/// A child entity of the <see cref="Workspace"/> aggregate, not an aggregate root of its own:
/// "the workspace always keeps at least one owner" is a rule over <i>all</i> members at once, so
/// only the workspace can enforce it. That is why the constructor and <see cref="ChangeRole"/>
/// are internal — nothing outside the aggregate can create a member or change a role behind
/// the workspace's back.
/// <para>
/// Not <see cref="IAuditable"/>: the row carries <c>joined_at</c> instead of
/// <c>created_at</c>/<c>updated_at</c> (database-schema.md).
/// </para>
/// </remarks>
public sealed class WorkspaceMember : Entity
{
    internal WorkspaceMember(Guid id, Guid workspaceId, Guid userId, WorkspaceRole role, DateTimeOffset joinedAt)
        : base(id)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
    }

    /// <summary>Parameterless constructor for EF Core's materialization only.</summary>
    private WorkspaceMember()
    {
    }

    public Guid WorkspaceId { get; private set; }

    public Guid UserId { get; private set; }

    public WorkspaceRole Role { get; private set; }

    /// <summary>UTC. Passed in by the caller — the domain has no clock (backend-architecture.md).</summary>
    public DateTimeOffset JoinedAt { get; private set; }

    public bool IsOwner => Role == WorkspaceRole.Owner;

    internal void ChangeRole(WorkspaceRole role) => Role = role;
}
