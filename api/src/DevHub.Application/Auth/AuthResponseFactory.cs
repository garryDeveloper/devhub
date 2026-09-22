using DevHub.Application.Auth.Contracts;
using DevHub.Application.Users.Contracts;
using DevHub.Domain.Users;

namespace DevHub.Application.Auth;

/// <summary>
/// Builds the <see cref="AuthResponse"/> register and login both return, and stages the refresh
/// token row that backs it.
/// </summary>
/// <remarks>
/// It stages rather than saves on purpose: the handler's single <c>SaveChangesAsync</c> writes
/// the user and its first refresh token in one transaction, so a failed registration can never
/// leave a token behind for a user that does not exist.
/// </remarks>
public sealed class AuthResponseFactory(ITokenService tokenService, IRefreshTokenRepository refreshTokens)
{
    public AuthResponse Create(User user)
    {
        var accessToken = tokenService.CreateAccessToken(user);
        var refreshToken = tokenService.CreateRefreshToken(user);

        refreshTokens.Add(refreshToken.Entity);

        return new AuthResponse(
            accessToken.Value,
            (int)accessToken.Lifetime.TotalSeconds,
            refreshToken.Value,
            UserDto.From(user));
    }
}
