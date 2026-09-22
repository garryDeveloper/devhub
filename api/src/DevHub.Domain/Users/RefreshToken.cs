using DevHub.Domain.Common;

namespace DevHub.Domain.Users;

/// <summary>
/// One issued refresh token (auth-spec.md §4). Only its SHA-256 hash is stored: a leaked
/// database dump must not hand out working sessions.
/// </summary>
/// <remarks>
/// DEVHUB-014/015 only <i>issue</i> refresh tokens — every register and login writes one row.
/// Rotation, revocation and reuse detection (<see cref="RevokedAt"/>,
/// <see cref="ReplacedByTokenId"/>) are DEVHUB-016's behaviour; the columns exist now so the
/// table is created once, in its final shape, like <c>users</c> was in DEVHUB-006.
/// <para>
/// Not an aggregate root: a refresh token has no life of its own outside its user's session and
/// raises no domain events. Not <see cref="IAuditable"/> either — the row is never updated in
/// place except to revoke it, so it carries only <c>created_at</c>.
/// </para>
/// </remarks>
public sealed class RefreshToken : Entity
{
    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    /// <summary>Parameterless constructor for EF Core's materialization only.</summary>
    private RefreshToken()
    {
        TokenHash = null!;
    }

    public Guid UserId { get; private set; }

    /// <summary>Lowercase hex SHA-256 of the raw token. The raw value is never stored.</summary>
    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset issuedAt, TimeSpan lifetime)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("A refresh token must belong to a user.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (lifetime <= TimeSpan.Zero)
        {
            throw new DomainException("A refresh token lifetime must be positive.");
        }

        return new RefreshToken(Guid.CreateVersion7(), userId, tokenHash, issuedAt + lifetime, issuedAt);
    }
}
