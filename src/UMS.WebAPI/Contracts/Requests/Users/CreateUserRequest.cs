using System.Collections.Generic;

namespace UMS.WebAPI.Contracts.Requests.Users
{
    public sealed record CreateUserRequest(
        string Email,
        string FirstName,
        string LastName,
        List<byte> RoleIds);
}
