using Microsoft.AspNetCore.Authorization;

namespace UMS.Infrastructure.Authorization
{
    /// <summary>
    /// Represents the requirement that a user must have a specific permission.
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}
