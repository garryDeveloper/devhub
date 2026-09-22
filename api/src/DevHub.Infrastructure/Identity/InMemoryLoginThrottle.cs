using System.Collections.Concurrent;
using DevHub.Application.Auth;
using DevHub.Application.Common;

namespace DevHub.Infrastructure.Identity;

/// <summary>
/// Login lockout (auth-spec.md §6): <see cref="MaxFailures"/> failures within a sliding
/// <see cref="Window"/> lock the account until the oldest of them ages out.
/// </summary>
/// <remarks>
/// DECISION (DEVHUB-015) — <b>in process memory, not the database</b>.
/// <para>
/// It is cheap, needs no migration, and uses <see cref="ITimeProvider"/> so tests can move time
/// instead of sleeping. The trade-off: state is per instance and lost on restart. With one API
/// instance (the MVP on Elastic Beanstalk) that is exact; with N instances behind a load
/// balancer an attacker gets up to N × 5 attempts per window. Revisit when EPIC 16 scales out.
/// The fix is a table behind this same <see cref="ILoginThrottle"/> port, with no handler change.
/// </para>
/// </remarks>
internal sealed class InMemoryLoginThrottle(ITimeProvider clock) : ILoginThrottle
{
    public const int MaxFailures = 5;

    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _failures = new(StringComparer.Ordinal);

    public TimeSpan? GetRemainingLockout(string normalizedEmail)
    {
        if (!_failures.TryGetValue(normalizedEmail, out var failures))
        {
            return null;
        }

        var now = clock.UtcNow;

        lock (failures)
        {
            DiscardExpired(failures, now);

            if (failures.Count == 0)
            {
                // Keeps the dictionary from growing forever with every email anyone ever
                // mistyped. A RecordFailure racing on this same queue can lose that one failure
                // to the removal — at worst one extra attempt, never a bypass.
                _failures.TryRemove(KeyValuePair.Create(normalizedEmail, failures));
                return null;
            }

            return failures.Count >= MaxFailures
                ? failures.Peek() + Window - now
                : null;
        }
    }

    public void RecordFailure(string normalizedEmail)
    {
        var failures = _failures.GetOrAdd(normalizedEmail, _ => new Queue<DateTimeOffset>());
        var now = clock.UtcNow;

        lock (failures)
        {
            DiscardExpired(failures, now);
            failures.Enqueue(now);
        }
    }

    public void Reset(string normalizedEmail) => _failures.TryRemove(normalizedEmail, out _);

    private static void DiscardExpired(Queue<DateTimeOffset> failures, DateTimeOffset now)
    {
        while (failures.Count > 0 && failures.Peek() + Window <= now)
        {
            failures.Dequeue();
        }
    }
}
