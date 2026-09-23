using DevHub.Application.Auth.Contracts;
using DevHub.Application.Common;
using DevHub.Application.Users;
using Microsoft.Extensions.Logging;

namespace DevHub.Application.Auth.Refresh;

/// <summary>
/// Exchanges a refresh token for a new access token <b>and</b> a new refresh token, retiring the
/// one presented (auth-spec.md §4).
/// </summary>
/// <remarks>
/// Every failure — unknown, expired, revoked, lost race — is the same
/// <see cref="InvalidRefreshToken"/>. The client's only correct reaction to any of them is "send
/// the user to login", so distinguishing them would help nobody but an attacker.
/// </remarks>
public sealed partial class RefreshHandler(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    ITokenService tokenService,
    ITimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<RefreshHandler> logger)
    : ICommandHandler<RefreshCommand, Result<RefreshResponse>>
{
    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("auth.invalid_refresh_token", "The refresh token is invalid or has expired.");

    public async Task<Result<RefreshResponse>> HandleAsync(RefreshCommand command, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var token = await refreshTokens
            .GetByHashAsync(tokenService.HashRefreshToken(command.RefreshToken), cancellationToken)
            .ConfigureAwait(false);

        // Unknown — never issued, purged, or its user was deleted (the FK cascades).
        if (token is null)
        {
            return InvalidRefreshToken;
        }

        // Checked before expiry: a revoked token is a stronger signal than an expired one. It
        // was already exchanged once, so whoever sends it now is either the thief replaying it,
        // or the real client after a thief used it first. We cannot tell which, so the session
        // ends for both — the thief's copy stops working, and the real user has to log in again.
        if (token.RevokedAt is not null)
        {
            var revoked = await refreshTokens.RevokeFamilyAsync(token.FamilyId, now, cancellationToken).ConfigureAwait(false);
            LogReuseDetected(logger, token.UserId, token.FamilyId, token.Id, revoked);
            return InvalidRefreshToken;
        }

        if (!token.IsActive(now))
        {
            return InvalidRefreshToken;
        }

        var user = await users.GetByIdAsync(token.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            // Only if the user was deleted between the two queries above.
            return InvalidRefreshToken;
        }

        var accessToken = tokenService.CreateAccessToken(user);
        var refreshToken = tokenService.RotateRefreshToken(token);
        refreshTokens.Add(refreshToken.Entity);

        try
        {
            // One transaction: the old row's revocation and the successor's insert commit
            // together or not at all.
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            // A parallel refresh with the same token rotated it first. Not treated as reuse —
            // it read the token as active too — but it must not get a second successor either.
            // This is why clients must refresh single-flight (DEVHUB-021).
            return InvalidRefreshToken;
        }

        return new RefreshResponse(
            accessToken.Value,
            (int)accessToken.Lifetime.TotalSeconds,
            refreshToken.Value);
    }

    // A security event (auth-spec.md §4). Ids only — never the token, raw or hashed.
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Refresh token reuse detected for user {UserId}: token {TokenId} was presented after rotation. "
            + "Revoked session {FamilyId} ({RevokedCount} active token(s)).")]
    private static partial void LogReuseDetected(
        ILogger logger, Guid userId, Guid familyId, Guid tokenId, int revokedCount);
}
