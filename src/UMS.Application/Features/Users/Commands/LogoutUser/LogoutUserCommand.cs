using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.LogoutUser
{
    /// <summary>
    /// Command to log out a user by invalidating their refresh token.
    /// </summary>
    public sealed record LogoutUserCommand(string RefreshToken) : ICommand;
}
