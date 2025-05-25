using System;
using UMS.Domain.Primitives;

namespace UMS.Domain.Users.Events
{
    public sealed record UserCreatedDomainEvent(
        Guid UserId,
        string Email,
        string UserCode,
        DateTime CreatedAtUtc) : DomainEvent(Guid.NewGuid());
}
