namespace DevHub.Domain.Common;

/// <summary>
/// Base class for anything with an identity. Two entities are the same entity when their
/// ids match, regardless of the rest of their state — that is what separates an entity
/// from a value object.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("An entity id cannot be empty.");
        }

        Id = id;
    }

    /// <summary>Parameterless constructor for EF Core's materialization only.</summary>
    protected Entity()
    {
    }

    public Guid Id { get; protected set; }

    public bool Equals(Entity? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Different aggregates can share an id only by accident; comparing the runtime type
        // keeps an Issue with id X from equalling a Project with id X.
        return GetType() == other.GetType() && Id == other.Id;
    }

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
