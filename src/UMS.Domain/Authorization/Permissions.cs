using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UMS.Domain.Authorization
{
    /// <summary>
    /// Defines all available permissions in the system.
    /// This static class is the single source of truth for permission strings.
    /// </summary>
    public static class Permissions
    {
        // Grouping permissions by feature
        public static class Users
        {
            public const string Read = "users:read";
            public const string Create = "users:create";
            public const string Update = "users:update";
            public const string Delete = "users:delete";
            public const string AssignRole = "users:assign_role";
            public const string ManageStatus = "users:manage_status";
        }
        
        public static class Roles
        {
            public const string Read = "roles:read";
            public const string Create = "roles:create";
            public const string Update = "roles:update";
            public const string Delete = "roles:delete";
            public const string AssignPermissions = "roles:assign_permissions";
        }
        
        public static class Clients
        {
            public const string Read = "clients:read";
            public const string Create = "clients:create";
            public const string Update = "clients:update";
            public const string Delete = "clients:delete";
            public const string ManagePermissions = "clients:manage_permissions";
        }

        // A helper method to  get all defined permission strings
        public static IReadOnlyList<string> GetAllPermissionValues()
        {
            var allPermissions = new List<string>();
            var nestedClasses = typeof(Permissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            foreach (var type in nestedClasses)
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string));

                allPermissions.AddRange(fields.Select(fi => (string)fi.GetRawConstantValue()!));
            }
            return allPermissions.AsReadOnly();
        }
    }
}
