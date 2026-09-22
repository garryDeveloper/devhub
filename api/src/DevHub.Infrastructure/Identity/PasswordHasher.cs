using DevHub.Application.Common;
using DevHub.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace DevHub.Infrastructure.Identity;

/// <summary>
/// PBKDF2 via ASP.NET Core's own <see cref="PasswordHasher{TUser}"/> (auth-spec.md §2) — no
/// bespoke crypto (technical notes, DEVHUB-013). The current format (v3) is PBKDF2-HMACSHA256
/// with a 128-bit per-user salt and 100,000 iterations; those defaults are what "sane defaults"
/// means here, and they're configurable later via <see cref="PasswordHasherOptions"/> without
/// this port's callers noticing.
/// </summary>
/// <remarks>
/// <see cref="PasswordHasher{TUser}"/> asks for a <typeparamref name="TUser"/> instance on every
/// call, but its default v2/v3 implementation never reads it — the parameter exists only so a
/// subclass could vary hashing per user. <see cref="IPasswordHasher"/> hashes a password before
/// any <see cref="User"/> exists (registration) and verifies one without needing the aggregate
/// loaded, so this adapter never has a real instance to pass and passes <see langword="null"/>!
/// instead of plumbing one through the port just to satisfy the signature.
/// </remarks>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(null!, hash, password) switch
        {
            // A hasher upgrade (e.g. raising the iteration count) flags existing rows as
            // needing a rehash without failing the login that triggered the check — the caller
            // still gets `true`. Re-hashing on that signal is the login handler's job
            // (DEVHUB-014/015), not this port's.
            PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded => true,
            PasswordVerificationResult.Failed => false,
            var result => throw new NotSupportedException($"Unhandled {nameof(PasswordVerificationResult)}: {result}."),
        };
}
