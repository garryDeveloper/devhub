using DevHub.Application.Common;

namespace DevHub.Infrastructure.Time;

/// <summary>
/// The production clock. The only place in the codebase allowed to read the real time.
/// </summary>
/// <remarks>
/// Everything else depends on <see cref="ITimeProvider"/>, so tests can freeze time and assert
/// on expiry, ordering and audit timestamps instead of sleeping and hoping.
/// </remarks>
internal sealed class SystemTimeProvider : ITimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
