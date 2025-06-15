using System;
using UMS.Domain.Primitives;

namespace UMS.Domain.Users.Events
{
    public sealed record UserRoleAssignedDomainEvent(
        Guid UserId,
        int RoleId) : DomainEvent(Guid.NewGuid());
}
