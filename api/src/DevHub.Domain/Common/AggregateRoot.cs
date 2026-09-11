namespace DevHub.Domain.Common;

/// <summary>
/// The entry point to a consistency boundary. Only aggregate roots are loaded and saved by
/// repositories, and only aggregate roots raise domain events.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(Guid id)
        : base(id)
    {
    }

    /// <summary>Parameterless constructor for EF Core's materialization only.</summary>
    protected AggregateRoot()
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Records something that happened. Events are collected here and dispatched by the
    /// infrastructure after the transaction commits — the domain never dispatches anything
    /// itself, which is how it stays free of infrastructure.
    /// </summary>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
