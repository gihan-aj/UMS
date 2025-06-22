using Mediator;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Roles.Commands.UpdateRole
{
    /// <summary>
    /// Command to update the name of an existing role.
    /// </summary>
    public sealed record UpdateRoleCommand(byte RoleId, string NewName) : ICommand;
}
