using System;
using UMS.Domain.Clients;
using UMS.Domain.Primitives;

namespace UMS.Domain.Authorization
{
    /// <summary>
    /// Represents a single, granular permission in the system.
    /// </summary>
    public class Permission : Entity<short>
    {
        public string Name { get; private set; } = string.Empty;

        // Nullable foreign key to Client. If null, it's a system permission.
        public Guid? ClientId { get; private set; }

        public virtual Client? Client { get; private set; } // Navigation property

        // Private constructor for EF Core
        private Permission() { }

        public static Permission Create(short id, string name, Guid? clientId = null)
        {
            return new Permission { Id = id, Name = name, ClientId = clientId };
        }
    }
}
