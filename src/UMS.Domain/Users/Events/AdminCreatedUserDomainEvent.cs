using System;
using UMS.Domain.Primitives;

namespace UMS.Domain.Users.Events
{
    public sealed record AdminCreatedUserDomainEvent(
        Guid UserId,
        string Email,
        string UserCode,
        DateTime CreatedAtUtc,
        string ActivationToken) : DomainEvent(Guid.NewGuid());
}
