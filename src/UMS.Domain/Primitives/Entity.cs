using System;

namespace UMS.Domain.Primitives
{
    /// <summary>
    /// Base class for all entities in the domain.
    /// </summary>
    /// <typeparam name="TId">The type of the entity's identifier.</typeparam>
    public abstract class Entity<TId> : IEquatable<Entity<TId>>
        where TId : notnull
    {
        /// <summary>
        /// Gets the entity's identifier.
        /// </summary>
        public TId Id { get; protected init; } = default!; // init for immutability after creation

        protected Entity(TId id)
        {
            Id = id;
        }

        // Protected parameterless constructor for ORM/serialization
        protected Entity() { }

        public override bool Equals(object? obj)
        {
            if(obj is null || obj.GetType() != GetType() || obj is not Entity<TId> entity)
            {
                return false;
            }
            return entity.Id.Equals(Id);
        }

        public override int GetHashCode() => Id.GetHashCode();

        public bool Equals(Entity<TId>? other)
        {
            if(other is null || other.GetType() != GetType())
            {
                return false;
            }
            return other.Id.Equals(Id);
        }

        public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
    }
}
