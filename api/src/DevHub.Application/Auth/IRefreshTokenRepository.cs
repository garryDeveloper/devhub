using DevHub.Domain.Users;

namespace DevHub.Application.Auth;

/// <summary>Stages refresh token rows. DEVHUB-016 adds lookup and revocation.</summary>
public interface IRefreshTokenRepository
{
    void Add(RefreshToken refreshToken);
}
