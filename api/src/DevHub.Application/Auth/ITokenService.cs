using DevHub.Domain.Users;

namespace DevHub.Application.Auth;

/// <summary>
/// Issues the two tokens of auth-spec.md §1. A port: the JWT library and the random number
/// generator are Infrastructure's business.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// A signed JWT carrying identity only — <c>sub</c>, <c>email</c>, <c>name</c>, <c>jti</c> —
    /// never a role or a workspace (DEVHUB-015 technical notes).
    /// </summary>
    AccessToken CreateAccessToken(User user);

    /// <summary>
    /// A fresh opaque token. <see cref="IssuedRefreshToken.Value"/> goes to the client exactly once;
    /// <see cref="IssuedRefreshToken.Entity"/> holds only its hash and is what gets stored.
    /// </summary>
    IssuedRefreshToken CreateRefreshToken(User user);
}

public sealed record AccessToken(string Value, TimeSpan Lifetime);

public sealed record IssuedRefreshToken(string Value, RefreshToken Entity);
