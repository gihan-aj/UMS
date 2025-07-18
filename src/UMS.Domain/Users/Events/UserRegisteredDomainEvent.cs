using System;
using UMS.Domain.Primitives;

namespace UMS.Domain.Users.Events
{
    public sealed record UserRegisteredDomainEvent(
        Guid UserId,
        string Email,
        string UserCode,
        DateTime CreatedAtUtc,
        string ActivationToken) : DomainEvent(Guid.NewGuid());
}
