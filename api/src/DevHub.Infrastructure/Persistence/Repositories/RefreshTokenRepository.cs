using DevHub.Application.Auth;
using DevHub.Domain.Users;

namespace DevHub.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(DevHubDbContext dbContext) : IRefreshTokenRepository
{
    public void Add(RefreshToken refreshToken) => dbContext.RefreshTokens.Add(refreshToken);
}
