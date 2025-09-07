using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Roles.Commands.DeleteRole
{
    public sealed record DeleteRoleCommand(byte Id) : ICommand;
}
