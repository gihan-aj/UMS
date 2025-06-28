using System.Collections.Generic;

namespace UMS.Domain.Primitives
{
    public interface IAggregateRoot
    {
        IReadOnlyCollection<DomainEvent> GetDomainEvents();
        void ClearDomainEvents();
    }
}
