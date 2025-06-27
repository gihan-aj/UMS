using System;

namespace UMS.WebAPI.Contracts.Responses.Users
{
    public record UserLoginResponse(
        Guid UserId,
        string Email,
        string UserCode,
        string Token,
        DateTime TokenExpiryUtc);
}
