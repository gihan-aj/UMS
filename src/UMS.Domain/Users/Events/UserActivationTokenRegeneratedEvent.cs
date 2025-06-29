using System;
using UMS.Domain.Primitives;

namespace UMS.Domain.Users.Events
{
    public sealed record UserActivationTokenRegeneratedEvent(
        Guid UserId,
        string Email,
        string ActivationToken) : DomainEvent(Guid.NewGuid());
}
