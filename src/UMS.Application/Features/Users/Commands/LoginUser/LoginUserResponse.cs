using System;

namespace UMS.Application.Features.Users.Commands.LoginUser
{
    public sealed record LoginUserResponse(
        Guid UserId,
        string Email,
        string UserCode,
        string Token, // The JWT
        DateTime TokenExpiryUtc,
        string RefreshToken);
}
