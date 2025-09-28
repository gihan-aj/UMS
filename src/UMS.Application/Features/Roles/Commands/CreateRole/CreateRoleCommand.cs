using System.Collections.Generic;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Roles.Commands.CreateRole
{
    /// <summary>
    /// Command to create a new role.
    /// </summary>
    public sealed record CreateRoleCommand(string Name, string? Description, List<string> PermissionNames) : ICommand<byte>;
}
