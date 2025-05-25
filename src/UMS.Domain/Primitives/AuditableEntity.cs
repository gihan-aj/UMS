using System;

namespace UMS.Domain.Primitives
{
    /// <summary>
    /// Base class for entities that require auditing information.
    /// </summary>
    /// <typeparam name="TId">The type of the entity's identifier.</typeparam>
    public abstract class AuditableEntity<TId> : Entity<TId>
        where TId : notnull
    {
        public DateTime CreatedAtUtc { get; protected set; }

        public Guid? CreatedBy { get; protected set; } // Or string, depending on how you track users

        public DateTime? LastModifiedAtUtc { get; protected set; }

        public Guid? LastModifiedBy { get; protected set; } // Or string

        protected AuditableEntity(TId id) : base(id) { }

        // Protected parameterless constructor for ORM/serialization
        protected AuditableEntity() { }

        // Methods to update audit properties, typically called by ORM interceptors or domain services
        public virtual void SetCreationAudit(Guid? userId)
        {
            CreatedAtUtc = DateTime.UtcNow;
            CreatedBy = userId;
        }

        public virtual void SetModificationAudit(Guid? userId)
        {
            LastModifiedAtUtc = DateTime.UtcNow;
            LastModifiedBy = userId;
        }
    }
}
