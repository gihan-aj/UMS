using System.Collections.Generic;

namespace UMS.WebAPI.Contracts.Requests.Roles
{
    public record UpdateRoleRequest(string Name, string? Description, List<string> PermissionNames);
}
