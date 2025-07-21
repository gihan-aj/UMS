using System.Collections.Generic;

namespace UMS.Application.Features.Permissions.Queries.ListPermissions
{
    public sealed record PermissionGroupResponse(
        string GroupName,
        List<PermissionDetailsResponse> Permissions);
}
