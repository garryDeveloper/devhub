using DevHub.Domain.Common;

namespace DevHub.Domain.Workspaces;

/// <summary>
/// The tenancy boundary of DevHub. Every project, issue and deployment belongs to exactly one
/// workspace, and every authorization check resolves to "is this user a member of it, and in
/// what role".
/// </summary>
/// <remarks>
/// The aggregate owns its <see cref="Members"/>: the list is a private field exposed read-only,
/// and every change goes through a method here. That is the only way to guarantee the central
/// invariant — <b>there is always at least one <see cref="WorkspaceRole.Owner"/></b> — because
/// it is a rule over the whole collection, not over any single member.
/// </remarks>
public sealed class Workspace : AggregateRoot, IAuditable
{
    public const int NameMaxLength = 80;

    private readonly List<WorkspaceMember> _members = [];

    private Workspace(Guid id, string name, string slug, Guid ownerId)
        : base(id)
    {
        Name = name;
        Slug = slug;
        OwnerId = ownerId;
    }

    /// <summary>Parameterless constructor for EF Core's materialization only.</summary>
    private Workspace()
    {
        Name = null!;
        Slug = null!;
    }

    public string Name { get; private set; }

    /// <summary>Unique across the system and immutable: there is no method that changes it.</summary>
    public string Slug { get; private set; }

    /// <summary>
    /// The user who created the workspace. Ownership itself is the <see cref="WorkspaceRole.Owner"/>
    /// role on <see cref="Members"/> — a workspace may have several owners, and this user may
    /// later be demoted or leave. Authorization never reads this column.
    /// </summary>
    public Guid OwnerId { get; private set; }

    public IReadOnlyCollection<WorkspaceMember> Members => _members.AsReadOnly();

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a workspace with <paramref name="ownerId"/> as its first and only
    /// <see cref="WorkspaceRole.Owner"/>. Owner and workspace are created in the same operation,
    /// so an ownerless workspace never exists, not even for an instant.
    /// </summary>
    /// <param name="slug">Optional. When omitted it is derived from <paramref name="name"/>.</param>
    public static Workspace Create(string name, Guid ownerId, DateTimeOffset now, string? slug = null)
    {
        var normalizedName = NormalizeName(name);

        if (slug is null)
        {
            slug = WorkspaceSlug.FromName(normalizedName);
        }
        else
        {
            WorkspaceSlug.Validate(slug);
        }

        var workspace = new Workspace(Guid.CreateVersion7(), normalizedName, slug, ownerId);
        workspace.AddMember(ownerId, WorkspaceRole.Owner, now);

        return workspace;
    }

    public void Rename(string name) => Name = NormalizeName(name);

    public WorkspaceMember AddMember(Guid userId, WorkspaceRole role, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("A workspace member must be a user.");
        }

        EnsureDefined(role);

        // The unique index on (workspace_id, user_id) catches this too, but only at SaveChanges
        // and only as a database error. Checking here gives the handler a domain rule to test.
        if (_members.Any(member => member.UserId == userId))
        {
            throw new DomainException("The user is already a member of this workspace.");
        }

        var member = new WorkspaceMember(Guid.CreateVersion7(), Id, userId, role, now);
        _members.Add(member);

        return member;
    }

    public void ChangeMemberRole(Guid memberId, WorkspaceRole role)
    {
        EnsureDefined(role);

        var member = GetMember(memberId);

        if (member.Role == role)
        {
            return;
        }

        if (member.IsOwner && IsLastOwner(member))
        {
            throw new DomainException("The last owner of a workspace cannot be demoted.");
        }

        member.ChangeRole(role);
    }

    public void RemoveMember(Guid memberId)
    {
        var member = GetMember(memberId);

        if (member.IsOwner && IsLastOwner(member))
        {
            throw new DomainException("The last owner of a workspace cannot be removed.");
        }

        _members.Remove(member);
    }

    private WorkspaceMember GetMember(Guid memberId) =>
        _members.SingleOrDefault(member => member.Id == memberId)
            ?? throw new DomainException("The member does not belong to this workspace.");

    private bool IsLastOwner(WorkspaceMember member) =>
        !_members.Any(other => other.IsOwner && other.Id != member.Id);

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException($"A workspace name cannot be longer than {NameMaxLength} characters.");
        }

        return trimmed;
    }

    // An enum is only an int: (WorkspaceRole)42 compiles, and would otherwise hit the CHECK
    // constraint as a database error instead of a domain one.
    private static void EnsureDefined(WorkspaceRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainException($"'{role}' is not a workspace role.");
        }
    }
}
