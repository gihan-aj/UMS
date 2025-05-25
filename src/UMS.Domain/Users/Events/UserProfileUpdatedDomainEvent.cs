using System;
using UMS.Domain.Primitives;

namespace UMS.Domain.Users.Events
{
    public sealed record UserProfileUpdatedDomainEvent(
        Guid UserId, 
        string? FirstName,
        string? LastName,
        DateTime UserProfileUpdatedAtUtc) : DomainEvent(Guid.NewGuid());


}
