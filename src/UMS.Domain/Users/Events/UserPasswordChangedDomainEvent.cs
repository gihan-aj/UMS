using System;
using UMS.Domain.Primitives;

namespace UMS.Domain.Users.Events
{
    public sealed record UserPasswordChangedDomainEvent(
        Guid UserId, 
        DateTime PasswordChangedAtUtc) : DomainEvent(Guid.NewGuid());


}
