using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.ResendActivationEmail
{
    /// <summary>
    /// Command to resend an activation email to a user.
    /// </summary>
    public sealed record ResendActivationEmailCommand(string Email) : ICommand;
}
