using DevHub.Application.Common;
using Microsoft.Extensions.Logging;

namespace DevHub.Application.Auth.Logout;

/// <summary>
/// Ends the session the presented refresh token belongs to, by revoking its whole family
/// (auth-spec.md §4).
/// </summary>
/// <remarks>
/// Always succeeds. Logout is idempotent — the second call finds the family already dead — and
/// answering differently for an unknown, expired or revoked token would turn this endpoint into
/// an oracle for token validity. The client clears its local state whatever the answer.
/// <para>
/// The family, not just the one token: a client may present a token that was already rotated
/// away (a stale copy in another tab), and the user still expects to be logged out. It is also
/// the same single indexed UPDATE that reuse detection uses.
/// </para>
/// <para>
/// What this cannot do: revoke access tokens already issued. They stay valid until they expire
/// (≤ 15 minutes) — the accepted cost of stateless JWTs.
/// </para>
/// </remarks>
public sealed partial class LogoutHandler(
    IRefreshTokenRepository refreshTokens,
    ITokenService tokenService,
    ITimeProvider clock,
    ILogger<LogoutHandler> logger)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            return Result.Success();
        }

        var token = await refreshTokens
            .GetByHashAsync(tokenService.HashRefreshToken(command.RefreshToken), cancellationToken)
            .ConfigureAwait(false);

        if (token is null)
        {
            return Result.Success();
        }

        // Immediate, not staged for SaveChanges: there is nothing else in this request to commit
        // with it. Returns 0 when the session was already ended — a repeated logout.
        var revoked = await refreshTokens.RevokeFamilyAsync(token.FamilyId, clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);

        if (revoked > 0)
        {
            LogLoggedOut(logger, token.UserId, token.FamilyId);
        }

        return Result.Success();
    }

    // Ids only — never the token, raw or hashed.
    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} logged out: session {FamilyId} revoked.")]
    private static partial void LogLoggedOut(ILogger logger, Guid userId, Guid familyId);
}
