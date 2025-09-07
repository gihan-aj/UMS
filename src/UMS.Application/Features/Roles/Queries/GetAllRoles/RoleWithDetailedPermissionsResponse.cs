using System.Collections.Generic;
using UMS.Application.Features.Permissions.Queries.ListPermissions;

namespace UMS.Application.Features.Roles.Queries.GetAllRoles
{
    public sealed record RoleWithDetailedPermissionsResponse(byte Id, string Name, List<PermissionDetailResponse> Permissions);
}
