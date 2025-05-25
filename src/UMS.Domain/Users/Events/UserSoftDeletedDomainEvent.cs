using System;
using UMS.Domain.Primitives;

namespace UMS.Domain.Users.Events
{
    public sealed record UserSoftDeletedDomainEvent(
        Guid UserId,
        DateTime UserSoftDeletedAtUtc) : DomainEvent(Guid.NewGuid());


}
