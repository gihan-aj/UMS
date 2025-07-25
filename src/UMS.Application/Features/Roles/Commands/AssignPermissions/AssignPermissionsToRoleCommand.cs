using System.Collections.Generic;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Roles.Commands.AssignPermissions
{
    /// <summary>
    /// Command to assign a set of permissions to a role.
    /// This will replace all existing permissions for the role.
    /// </summary>
    public sealed record AssignPermissionsToRoleCommand(
        byte RoleId,
        List<string> PermissionNames) : ICommand;
}
