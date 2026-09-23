using DevHub.Domain.Users;

namespace DevHub.Application.Auth;

/// <summary>Loads, stages and revokes refresh token rows (auth-spec.md §4).</summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Looks a token up by the SHA-256 of its raw value — the only way in. The index on
    /// <c>token_hash</c> is unique, and the attacker cannot choose the bytes being compared, so
    /// the lookup's timing reveals nothing about stored tokens.
    /// </summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(RefreshToken refreshToken);

    /// <summary>
    /// Revokes every still-active token of a family, <b>immediately</b> — a single UPDATE, not
    /// staged for <see cref="Common.IUnitOfWork.SaveChangesAsync"/>. Reuse detection answers 401
    /// right after, and the revocation must not depend on anything else in the request
    /// succeeding.
    /// </summary>
    /// <returns>How many tokens were revoked; 0 when the family was already dead.</returns>
    Task<int> RevokeFamilyAsync(Guid familyId, DateTimeOffset revokedAt, CancellationToken cancellationToken);
}
