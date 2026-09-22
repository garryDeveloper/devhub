using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using DevHub.Application.Auth;
using DevHub.Application.Common;
using DevHub.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DevHub.Infrastructure.Identity;

/// <summary>
/// HS256 access tokens and opaque refresh tokens (auth-spec.md §1, §3).
/// </summary>
internal sealed class TokenService(IOptions<JwtOptions> options, ITimeProvider clock) : ITokenService
{
    // JsonWebTokenHandler rather than the older JwtSecurityTokenHandler: it is what JwtBearer
    // validates with since .NET 8, so issuing and validating go through the same code.
    private static readonly JsonWebTokenHandler Handler = new();

    public AccessToken CreateAccessToken(User user)
    {
        var jwt = options.Value;
        var now = clock.UtcNow;
        var lifetime = TimeSpan.FromMinutes(jwt.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = (now + lifetime).UtcDateTime,

            // Identity only. No role, no workspace: roles are read from the database per
            // request, so a membership change takes effect on the next call, not 15 minutes later.
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email,
                [JwtRegisteredClaimNames.Name] = user.DisplayName,

                // Unique per token, so one specific token can be told apart in logs.
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            },

            SigningCredentials = new SigningCredentials(JwtSigningKey.From(jwt), SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(Handler.CreateToken(descriptor), lifetime);
    }

    public IssuedRefreshToken CreateRefreshToken(User user)
    {
        // 256 bits from the OS CSPRNG. Never Random, never a Guid: a v4 Guid carries only 122
        // random bits, and a v7 Guid is partly a timestamp.
        var value = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

        var entity = RefreshToken.Issue(
            user.Id,
            Hash(value),
            clock.UtcNow,
            TimeSpan.FromDays(options.Value.RefreshTokenDays));

        return new IssuedRefreshToken(value, entity);
    }

    /// <summary>
    /// SHA-256, not PBKDF2: the input is 256 random bits, not a human-chosen password, so there
    /// is nothing to brute-force and no reason to make every refresh slow. DEVHUB-016 reuses this
    /// to look up a presented token.
    /// </summary>
    internal static string Hash(string refreshToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
