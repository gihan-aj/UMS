using System;
using UMS.Domain.Primitives;

namespace UMS.Domain.Users.Events
{
    public sealed record UserAccountDeactivatedDomainEvent(
        Guid UserId, 
        DateTime DeactivtedAtUtc) : DomainEvent(Guid.NewGuid());


}
