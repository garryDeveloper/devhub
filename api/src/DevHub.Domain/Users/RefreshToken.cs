using DevHub.Domain.Common;

namespace DevHub.Domain.Users;

/// <summary>
/// One issued refresh token (auth-spec.md §4). Only its SHA-256 hash is stored: a leaked
/// database dump must not hand out working sessions.
/// </summary>
/// <remarks>
/// Every login or registration starts a new <b>family</b> (<see cref="Issue"/>); every refresh
/// replaces the current token with a successor in the same family (<see cref="Rotate"/>). So a
/// family is one session, and at most one of its tokens is active at a time. Presenting a token
/// that was already rotated away means two parties hold the same session — and the whole family
/// is revoked (DEVHUB-016).
/// <para>
/// Not an aggregate root: a refresh token has no life of its own outside its user's session and
/// raises no domain events. Not <see cref="IAuditable"/> either — the row is never updated in
/// place except to revoke it, so it carries only <c>created_at</c>.
/// </para>
/// </remarks>
public sealed class RefreshToken : Entity
{
    private RefreshToken(
        Guid id, Guid userId, Guid familyId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt)
        : base(id)
    {
        UserId = userId;
        FamilyId = familyId;
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

    /// <summary>
    /// The session this token belongs to, shared by the first token of a login and every token
    /// rotated from it. Reuse detection revokes by family in one indexed UPDATE rather than
    /// walking <see cref="ReplacedByTokenId"/> link by link — a 30-day session refreshed every
    /// 15 minutes is a chain of ~2,900 rows.
    /// </summary>
    public Guid FamilyId { get; private set; }

    /// <summary>Lowercase hex SHA-256 of the raw token. The raw value is never stored.</summary>
    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// The successor issued when this token was rotated. Not needed for revocation any more
    /// (<see cref="FamilyId"/> does that); kept as the audit trail of the session.
    /// </summary>
    public Guid? ReplacedByTokenId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Starts a new session: the first token of a new family.</summary>
    public static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset issuedAt, TimeSpan lifetime)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("A refresh token must belong to a user.");
        }

        // The first token's id doubles as the family id: the session is named after the token
        // that started it, and needs no second generated value.
        var id = Guid.CreateVersion7();
        return Create(id, userId, familyId: id, tokenHash, issuedAt, lifetime);
    }

    /// <summary>Usable to obtain new tokens: neither revoked nor expired.</summary>
    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    /// <summary>
    /// Replaces this token with its successor in the same family, and revokes this one. Both
    /// happen here, together, so no caller can issue a successor and forget to retire the
    /// original — which would leave two live tokens for one session.
    /// </summary>
    /// <remarks>
    /// The successor gets a full new lifetime: a session lives as long as it keeps being used.
    /// An absolute cap on session length would be a rule on the family, not on one token.
    /// </remarks>
    public RefreshToken Rotate(string successorTokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        if (!IsActive(now))
        {
            // The handler checks first and answers 401; reaching here is a bug, not bad input.
            throw new DomainException("Only an active refresh token can be rotated.");
        }

        var successor = Create(Guid.CreateVersion7(), UserId, FamilyId, successorTokenHash, now, lifetime);

        RevokedAt = now;
        ReplacedByTokenId = successor.Id;

        return successor;
    }

    private static RefreshToken Create(
        Guid id, Guid userId, Guid familyId, string tokenHash, DateTimeOffset issuedAt, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (lifetime <= TimeSpan.Zero)
        {
            throw new DomainException("A refresh token lifetime must be positive.");
        }

        return new RefreshToken(id, userId, familyId, tokenHash, issuedAt + lifetime, issuedAt);
    }
}
