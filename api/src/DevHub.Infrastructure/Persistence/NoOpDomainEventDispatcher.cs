using DevHub.Application.Common;
using DevHub.Domain.Common;

namespace DevHub.Infrastructure.Persistence;

/// <summary>
/// Accepts domain events and drops them. There are no handlers yet.
/// </summary>
/// <remarks>
/// A deliberate placeholder, not an oversight. It exists so <see cref="DevHubDbContext"/> can
/// have the collect-clear-save-dispatch order correct and covered from the first migration
/// onward, rather than growing it later around code that never had it.
/// <para>
/// The real implementation resolves <c>IDomainEventHandler&lt;T&gt;</c> from the container and
/// wraps each dispatch in try/catch — see backend-architecture.md §7. It arrives with the first
/// ticket that raises an event (EPIC 7, <c>issue_activities</c>).
/// </para>
/// </remarks>
internal sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
{
    public Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
