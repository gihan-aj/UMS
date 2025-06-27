using System.Collections.Generic;

namespace UMS.WebAPI.Contracts.Requests.Users
{
    public record SetUserRolesRequest(List<byte> RoleIds);
}
