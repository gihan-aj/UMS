using System.Collections.Generic;

namespace UMS.WebAPI.Contracts.Requests.Roles
{
    public record CreateRoleRequest(string Name, List<string> PermissionNames);
}
