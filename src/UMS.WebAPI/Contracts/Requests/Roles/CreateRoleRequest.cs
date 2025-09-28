using System.Collections.Generic;

namespace UMS.WebAPI.Contracts.Requests.Roles
{
    public record CreateRoleRequest(string Name, string? Description, List<string> PermissionNames);
}
