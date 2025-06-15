using System;
using UMS.Domain.Primitives;

namespace UMS.Domain.Authorization
{
    /// <summary>
    /// Represents a single, granular permission in the system.
    /// </summary>
    public class Permission : Entity<short>
    {
        public string Name { get; private set; } = string.Empty;

        // Private constructor for EF Core
        private Permission() { }

        public static Permission Create(short id, string name)
        {
            return new Permission { Id = id, Name = name };
        }
    }
}
