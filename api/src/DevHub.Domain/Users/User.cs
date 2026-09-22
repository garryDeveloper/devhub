using DevHub.Domain.Common;

namespace DevHub.Domain.Users;

/// <summary>
/// A person with an account. Everything else in DevHub hangs off a user: workspaces have an
/// owner, issues have a reporter and an assignee, activities have an actor.
/// </summary>
/// <remarks>
/// DEVHUB-006 maps the full row from docs/tech-specs/database-schema.md §2 so that the initial
/// migration creates the final <c>users</c> table, but deliberately stops short of behaviour.
/// <b>DEVHUB-013 owns the aggregate proper</b>: it replaces the constructor below with a
/// <c>User.Create(email, displayName, passwordHash)</c> factory that normalizes the email, and
/// adds <c>ChangeDisplayName</c>, <c>SetAvatar</c> and <c>ChangePassword</c>. Adding behaviour
/// here would empty that ticket out; adding columns there would mean a corrective migration for
/// a table that never reached production.
/// </remarks>
public sealed class User : AggregateRoot, IAuditable
{
    public User(Guid id, string email, string displayName, string passwordHash)
        : base(id)
    {
        // Guard clauses only — no normalization, no format validation. Those are rules, and
        // rules belong to DEVHUB-013's factory, tested there.
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
    }

    /// <summary>Parameterless constructor for EF Core's materialization only.</summary>
    private User()
    {
        Email = null!;
        DisplayName = null!;
        PasswordHash = null!;
    }

    /// <summary>
    /// Stored as <c>citext</c>, so uniqueness is case-insensitive in the database itself rather
    /// than by lowercasing on the way in and hoping every write path remembers to.
    /// </summary>
    public string Email { get; private set; }

    public string DisplayName { get; private set; }

    /// <summary>The S3 object key, not a URL. URLs are presigned at read time (EPIC 15).</summary>
    public string? AvatarKey { get; private set; }

    /// <summary>
    /// Never projected into a DTO and never logged. DEVHUB-013 adds the test that enforces it.
    /// </summary>
    public string PasswordHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static User Create(string email, string displayName, string passwordHash)
    {
        // Guarded here, not only in the constructor below: a null email would otherwise reach
        // Trim() first and throw NullReferenceException instead of the ArgumentException every
        // other invalid-input path throws.
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        // v7 for index locality (CLAUDE.md §4), matching every other aggregate's id generation.
        var id = Guid.CreateVersion7();
        return new User(id, NormalizeEmail(email), displayName, passwordHash);
    }

    /// <summary>
    /// The one definition of "the same email". Public so lookups (login, the registration
    /// pre-check, the login lockout key) normalize exactly the way <see cref="Create"/> stored it.
    /// </summary>
    public static string NormalizeEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return email.Trim().ToLowerInvariant();
    }

    public void ChangeDisplayName(string newDisplayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newDisplayName);
        DisplayName = newDisplayName;
    }

    public void SetAvatar(string? avatarKey)
    {
        AvatarKey = avatarKey;
    }

    public void ChangePassword(string newPasswordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);
        PasswordHash = newPasswordHash;
    }
}
