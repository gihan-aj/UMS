using Mediator;
using System.Collections.Generic;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Roles.Commands.UpdateRole
{
    /// <summary>
    /// Command to update the name of an existing role.
    /// </summary>
    public sealed record UpdateRoleCommand(byte Id, string NewName, List<string> PermissionNames) : ICommand;
}
