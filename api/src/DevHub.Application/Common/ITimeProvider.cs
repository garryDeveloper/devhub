namespace DevHub.Application.Common;

/// <summary>
/// The only source of "now" in the domain and application layers. Never call
/// DateTimeOffset.UtcNow directly in a handler or an entity: tests need to freeze time to
/// assert on expiry, ordering and audit timestamps.
/// </summary>
/// <remarks>
/// The BCL has System.TimeProvider since .NET 8. This narrower interface is kept because it is
/// trivial to fake and because it states the UTC contract in its signature.
/// </remarks>
public interface ITimeProvider
{
    DateTimeOffset UtcNow { get; }
}
