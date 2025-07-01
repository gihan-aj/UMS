using System;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.DeleteUser
{
    public sealed record DeleteUserCommand(Guid UserId) : ICommand;
}
