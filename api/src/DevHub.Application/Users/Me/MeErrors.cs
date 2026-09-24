using DevHub.Application.Common;

namespace DevHub.Application.Users.Me;

public static class MeErrors
{
    /// <summary>
    /// The token is valid but its user was deleted since it was issued (the ≤ 15 minute window,
    /// auth-spec.md §4). 401, not 404: there is no account behind this session any more, and 401
    /// is what makes every client drop its tokens and go to login. Same type as the framework's
    /// own 401 (auth-spec.md §6), because to the client it is the same situation.
    /// </summary>
    public static readonly Error AccountNotFound =
        Error.Unauthorized("auth.unauthenticated", "The account for this access token no longer exists.");
}
