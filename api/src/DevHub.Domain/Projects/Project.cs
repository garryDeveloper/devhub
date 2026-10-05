using DevHub.Domain.Common;

namespace DevHub.Domain.Projects;

/// <summary>
/// A unit of work inside a workspace: issues, environments, releases and CI runs all belong to
/// exactly one project. The project key (<c>DEV</c>) is what users see constantly in issue keys
/// (<c>DEV-42</c>), which is why <see cref="ProjectKey"/> validates it strictly and nothing here
/// ever changes it.
/// </summary>
/// <remarks>
/// The aggregate owns its <see cref="Members"/> the same way <c>Workspace</c> owns its members: a
/// private list exposed read-only, changed only through <see cref="AddMember"/> and
/// <see cref="RemoveMember"/>.
/// </remarks>
public sealed class Project : AggregateRoot, IAuditable
{
    public const int NameMaxLength = 80;
    public const int DescriptionMaxLength = 2000;
    public const int ColorMaxLength = 7;
    public const int IconMaxLength = 40;

    private readonly List<ProjectMember> _members = [];

    private Project(Guid id, Guid workspaceId, string name, string key)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Name = name;
        Key = key;
    }

    /// <summary>Parameterless constructor for EF Core's materialization only.</summary>
    private Project()
    {
        Name = null!;
        Key = null!;
    }

    public Guid WorkspaceId { get; private set; }

    public string Name { get; private set; }

    /// <summary>Unique within <see cref="WorkspaceId"/> and immutable: no method here changes it.</summary>
    public string Key { get; private set; }

    public string? Description { get; private set; }

    public string? Color { get; private set; }

    public string? Icon { get; private set; }

    /// <summary>The last issued issue number. Advances only through <see cref="NextIssueNumber"/>.</summary>
    public int IssueSequence { get; private set; }

    /// <summary>Null while active. An archived project keeps its data and can be restored.</summary>
    public DateTimeOffset? ArchivedAt { get; private set; }

    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a project with <paramref name="creatorId"/> as its first member — a memberless
    /// project would mean nobody could be assigned its first issue. Whether
    /// <paramref name="creatorId"/> is actually a member of <paramref name="workspaceId"/> is not
    /// checked here: that invariant spans two aggregates, so the handler (DEVHUB-031) checks it
    /// before calling this.
    /// </summary>
    public static Project Create(Guid workspaceId, string name, string key, Guid creatorId, DateTimeOffset now)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new DomainException("A project must belong to a workspace.");
        }

        var normalizedName = NormalizeName(name);
        var normalizedKey = NormalizeKey(key);

        var project = new Project(Guid.CreateVersion7(), workspaceId, normalizedName, normalizedKey);
        project.AddMember(creatorId, now);

        return project;
    }

    public void Rename(string name) => Name = NormalizeName(name);

    public void UpdateDescription(string? description) => Description = NormalizeDescription(description);

    public void SetAppearance(string? color, string? icon)
    {
        Color = NormalizeColor(color);
        Icon = NormalizeIcon(icon);
    }

    public void Archive(DateTimeOffset now)
    {
        if (ArchivedAt is not null)
        {
            throw new DomainException("The project is already archived.");
        }

        ArchivedAt = now;
    }

    public void Unarchive()
    {
        if (ArchivedAt is null)
        {
            throw new DomainException("The project is not archived.");
        }

        ArchivedAt = null;
    }

    /// <summary>
    /// Advances the issue sequence and returns the new number, for the caller to build
    /// <c>{Key}-{n}</c> from. This keeps "only the domain changes this counter" true; making the
    /// increment race-safe under concurrent issue creation is the repository's job — an atomic
    /// <c>UPDATE ... RETURNING</c> (database-schema.md §5) — once DEVHUB-036 adds it.
    /// </summary>
    public int NextIssueNumber() => ++IssueSequence;

    public ProjectMember AddMember(Guid userId, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("A project member must be a user.");
        }

        // The unique index on (project_id, user_id) catches this too, but only at SaveChanges
        // and only as a database error. Checking here gives the handler a domain rule to test.
        if (_members.Any(member => member.UserId == userId))
        {
            throw new DomainException("The user is already a member of this project.");
        }

        var member = new ProjectMember(Guid.CreateVersion7(), Id, userId, ProjectRole.Member, now);
        _members.Add(member);

        return member;
    }

    public void RemoveMember(Guid memberId)
    {
        var member = GetMember(memberId);
        _members.Remove(member);
    }

    private ProjectMember GetMember(Guid memberId) =>
        _members.SingleOrDefault(member => member.Id == memberId)
            ?? throw new DomainException("The member does not belong to this project.");

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException($"A project name cannot be longer than {NameMaxLength} characters.");
        }

        return trimmed;
    }

    private static string NormalizeKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var trimmed = key.Trim();
        ProjectKey.Validate(trimmed);

        return trimmed;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        if (trimmed.Length > DescriptionMaxLength)
        {
            throw new DomainException(
                $"A project description cannot be longer than {DescriptionMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var trimmed = color.Trim();
        if (trimmed.Length > ColorMaxLength)
        {
            throw new DomainException($"A project color cannot be longer than {ColorMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeIcon(string? icon)
    {
        if (string.IsNullOrWhiteSpace(icon))
        {
            return null;
        }

        var trimmed = icon.Trim();
        if (trimmed.Length > IconMaxLength)
        {
            throw new DomainException($"A project icon cannot be longer than {IconMaxLength} characters.");
        }

        return trimmed;
    }
}
