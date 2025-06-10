using System;
using UMS.Domain.Primitives;

namespace UMS.Domain.Users.Events
{
    public sealed record UserAccountActivatedDomainEvent(
        Guid UserId, 
        DateTime ActivtedAtUtc) : DomainEvent(Guid.NewGuid());
}
