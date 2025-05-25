using Mediator;
using System;

namespace UMS.Domain.Primitives
{
    /// <summary>
    /// Base class for domain events.
    /// Domain events are things that have happened in the past.
    /// </summary>
    public abstract record DomainEvent(Guid Id) : INotification;
}
