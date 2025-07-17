using System;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.SetInitialPassword
{
    public sealed record SetInitialPasswordCommand(
        string Email,
        string Token,
        string NewPassword,
        string ConfirmPassword) : ICommand;
}
