using System;
using UMS.Domain.Primitives;

namespace UMS.Domain.Users.Events
{
    public sealed record UserPasswordResetRequestedEvent(
        Guid UserId,
        string Email,
        string ResetToken) : DomainEvent(Guid.NewGuid());
}
