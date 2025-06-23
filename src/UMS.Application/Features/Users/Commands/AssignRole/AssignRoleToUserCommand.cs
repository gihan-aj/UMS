using System;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.AssignRole
{
    public sealed record AssignRoleToUserCommand(Guid UserId, byte RoleId) : ICommand;
}
