namespace DevHub.Application.Auth;

/// <summary>
/// Per-account lockout (auth-spec.md §6): too many failed logins for one email in a window, and
/// further attempts are refused with 429 until the window moves on. Not a permanent lock.
/// </summary>
/// <remarks>
/// Keyed by the normalized email whether or not an account exists. Keying only real accounts
/// would make "locked out" versus "invalid credentials" a way to discover which emails are
/// registered — the exact leak the generic 401 exists to prevent.
/// <para>
/// This is per <i>account</i>; the per-<i>IP</i> limit (10/min) is ASP.NET Core rate limiting in
/// DevHub.Api. The two stop different attacks: one address trying many accounts, and many
/// addresses trying one.
/// </para>
/// </remarks>
public interface ILoginThrottle
{
    /// <summary>How long until <paramref name="normalizedEmail"/> may try again; null if it may now.</summary>
    TimeSpan? GetRemainingLockout(string normalizedEmail);

    void RecordFailure(string normalizedEmail);

    void Reset(string normalizedEmail);
}
