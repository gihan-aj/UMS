using System;
using UMS.Domain.Primitives;

namespace UMS.Domain.Authorization.Events
{
    public sealed record RoleSoftDeletedDomainEvent(int RoleId) : DomainEvent(Guid.NewGuid());
}
