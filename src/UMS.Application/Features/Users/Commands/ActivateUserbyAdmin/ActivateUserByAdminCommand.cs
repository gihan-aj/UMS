using System;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.ActivateUserbyAdmin
{
    public sealed record ActivateUserByAdminCommand(Guid UserId) : ICommand;
}
