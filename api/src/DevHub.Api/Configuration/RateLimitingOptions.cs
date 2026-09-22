using System.ComponentModel.DataAnnotations;

namespace DevHub.Api.Configuration;

/// <summary>
/// Per-IP request limits (auth-spec.md §6). Configurable so integration tests, which all arrive
/// from the same in-memory "address", can raise the limit instead of tripping it.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Name of the policy on <c>POST /api/auth/register</c> and <c>/login</c>.</summary>
    public const string AuthPolicy = "auth";

    /// <summary>Requests per minute per client IP to the auth endpoints.</summary>
    [Range(1, 10_000)]
    public int AuthPermitLimit { get; init; } = 10;
}
