using System.ComponentModel.DataAnnotations;

namespace DevHub.Infrastructure.Identity;

/// <summary>
/// Access token signing configuration (auth-spec.md §3). DEVHUB-007 established the fail-fast
/// contract, so a missing secret is a boot-time error rather than a 500 on the first login;
/// DEVHUB-015 issues (<see cref="TokenService"/>) and validates (JwtBearer) with these values.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    // HS256 wants a 256-bit key. 32 raw ASCII characters is the floor, not a target — generate
    // with `openssl rand -base64 48` (local-development.md §4), never type one by hand.
    [Required]
    [MinLength(32)]
    public string Secret { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = "devhub-api";

    [Required]
    public string Audience { get; init; } = "devhub-clients";

    [Range(1, 60)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; init; } = 30;
}
