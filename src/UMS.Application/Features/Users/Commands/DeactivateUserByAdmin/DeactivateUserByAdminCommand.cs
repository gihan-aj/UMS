using System;
using UMS.Application.Common.Messaging.Commands;
using UMS.Application.Features.Users.Commands.ActivateUserbyAdmin;

namespace UMS.Application.Features.Users.Commands.DeactivateUserByAdmin
{
    public sealed record DeactivateUserByAdminCommand(Guid UserId) : ICommand;
}
