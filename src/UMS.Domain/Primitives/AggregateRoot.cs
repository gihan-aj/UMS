using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UMS.Domain.Primitives
{
    /// <summary>
    /// Base class for aggregate roots, providing support for domain events.
    /// </summary>
    /// <typeparam name="TId">The type of the aggregate root's identifier.</typeparam>
    public abstract class AggregateRoot<TId> : AuditableEntity<TId> , IAggregateRoot
        where TId : notnull
    {
        private readonly List<DomainEvent> _domainEvents = new();

        protected AggregateRoot(TId id) : base(id) { }

        // Protected parameterless constructor for ORM/serialization
        protected AggregateRoot() { }

        /// <summary>
        /// Gets the list of domain events raised by this aggregate.
        /// </summary>
        public IReadOnlyCollection<DomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

        /// <summary>
        /// Adds a domain event to the aggregate.
        /// </summary>
        /// <param name="domainEvent">The domain event to add.</param>
        public void RaiseDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// Clears all domain events from the aggregate.
        /// Called after events have been dispatched.
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
