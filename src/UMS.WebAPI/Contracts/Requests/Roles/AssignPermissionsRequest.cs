using System.Collections.Generic;

namespace UMS.WebAPI.Contracts.Requests.Roles
{
    public record AssignPermissionsRequest(List<string> PermissionNames);
}
