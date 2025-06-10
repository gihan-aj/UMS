using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.RequestPasswordReset
{
    public sealed record RequestPasswordResetCommand(string Email) : ICommand;
}
