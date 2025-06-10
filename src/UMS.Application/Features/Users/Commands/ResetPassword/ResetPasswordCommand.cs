using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.ResetPassword
{
    public sealed record ResetPasswordCommand(
        string Email,
        string Token,
        string NewPassword,
        string ConfirmPassword) : ICommand;
}
