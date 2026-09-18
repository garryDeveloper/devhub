using DevHub.Domain.Common;

namespace DevHub.Application.Common;

/// <summary>
/// Delivers domain events to their handlers after the transaction that produced them commits.
/// </summary>
/// <remarks>
/// A port, implemented in Infrastructure, because the DbContext is what knows when a save
/// succeeded — see docs/tech-specs/backend-architecture.md §7.
/// <para>
/// The ordering is the whole point: events are collected from the tracked aggregates and
/// cleared <i>before</i> <c>SaveChangesAsync</c>, then dispatched <i>after</i> it returns. An
/// event dispatched before the commit can announce something that then gets rolled back.
/// </para>
/// <para>
/// Handlers must be idempotent and must never throw into the request path. A missing
/// notification is an annoyance; a rolled-back status change is a bug. The one exception is
/// <c>issue_activities</c>, which is part of the audit contract and is written inside the same
/// transaction as the change that produced it, not here.
/// </para>
/// </remarks>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken);
}
