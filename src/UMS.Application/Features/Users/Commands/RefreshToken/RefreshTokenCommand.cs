using UMS.Application.Common.Messaging.Commands;
using UMS.Application.Features.Users.Commands.LoginUser;

namespace UMS.Application.Features.Users.Commands.RefreshToken
{
    /// <summary>
    /// Command to refresh an authentication session using a refresh token.
    /// </summary>
    public sealed record RefreshTokenCommand(
        string AccessToken, // Expired JWT
        string RefreshToken) : ICommand<LoginUserResponse>;
}
