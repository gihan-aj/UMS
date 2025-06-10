using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.LoginUser
{
    public record LoginUserCommand(
        string Email,
        string Password,
        string DeviceId) : ICommand<LoginUserResponse>;
}
