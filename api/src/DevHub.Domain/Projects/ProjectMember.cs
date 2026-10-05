using DevHub.Domain.Common;

namespace DevHub.Domain.Projects;

/// <summary>
/// A user's membership of one project. In the MVP it only narrows who can be assigned issues —
/// it does not restrict reading (DEVHUB-032): every workspace member can see every project.
/// </summary>
/// <remarks>
/// A child entity of the <see cref="Project"/> aggregate, not an aggregate root of its own — same
/// reasoning as <see cref="Workspaces.WorkspaceMember"/>. The constructor is internal so nothing
/// outside <see cref="Project"/> can create one.
/// <para>
/// Not <see cref="IAuditable"/>: the row carries <c>added_at</c> instead of
/// <c>created_at</c>/<c>updated_at</c> (database-schema.md).
/// </para>
/// <para>
/// "A project member must already be a workspace member" (domain-model.md) spans two
/// aggregates, so <see cref="Project"/> cannot enforce it by itself — the handler that adds a
/// member (DEVHUB-032) checks workspace membership first, through
/// <c>IWorkspaceAccessService</c>.
/// </para>
/// </remarks>
public sealed class ProjectMember : Entity
{
    internal ProjectMember(Guid id, Guid projectId, Guid userId, ProjectRole role, DateTimeOffset addedAt)
        : base(id)
    {
        ProjectId = projectId;
        UserId = userId;
        Role = role;
        AddedAt = addedAt;
    }

    /// <summary>Parameterless constructor for EF Core's materialization only.</summary>
    private ProjectMember()
    {
    }

    public Guid ProjectId { get; private set; }

    public Guid UserId { get; private set; }

    public ProjectRole Role { get; private set; }

    /// <summary>UTC. Passed in by the caller — the domain has no clock (backend-architecture.md).</summary>
    public DateTimeOffset AddedAt { get; private set; }
}
