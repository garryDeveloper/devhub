using DevHub.Application.Auth;
using DevHub.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DevHub.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(DevHubDbContext dbContext) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public void Add(RefreshToken refreshToken) => dbContext.RefreshTokens.Add(refreshToken);

    // ExecuteUpdate bypasses the change tracker and the entity's methods on purpose: this is a
    // bulk security action over rows the request never loaded, and "revoked_at IS NULL" keeps
    // the original revocation time of tokens that were already rotated away.
    public Task<int> RevokeFamilyAsync(Guid familyId, DateTimeOffset revokedAt, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAt, revokedAt),
                cancellationToken);
}
