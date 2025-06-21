using System.Collections.Generic;

namespace UMS.Application.Features.Roles.Queries.GetRoleById
{
    /// <summary>
    /// Response DTO containing detailed role information, including its permissions.
    /// </summary>
    public sealed record RoleWithPermissionsResponse(byte Id, string Name, List<PermissionResponse> Permissions);
}
