using System;
using UMS.Domain.Authorization;

namespace UMS.Domain.Users
{
    /// <summary>
    /// Join entity representing the many-to-many relationship between User and Role.
    /// </summary>
    public class UserRole
    {
        public Guid UserId { get; set; }

        public byte RoleId { get; set; }

        public virtual Role Role { get; set; } = null!;
    }
}
