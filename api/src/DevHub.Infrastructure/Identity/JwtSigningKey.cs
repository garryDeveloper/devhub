using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DevHub.Infrastructure.Identity;

/// <summary>
/// The one place the HS256 key is derived from configuration, used by both the issuer
/// (<see cref="TokenService"/>) and the validator (JwtBearer). Deriving it in two places is how
/// a token ends up signed with one key and checked against another.
/// </summary>
internal static class JwtSigningKey
{
    public static SymmetricSecurityKey From(JwtOptions options) => new(Encoding.UTF8.GetBytes(options.Secret));
}
