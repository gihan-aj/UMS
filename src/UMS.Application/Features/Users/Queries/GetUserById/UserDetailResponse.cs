using System;
using System.Collections.Generic;
using UMS.Application.Features.Permissions.Queries.ListPermissions;

namespace UMS.Application.Features.Users.Queries.GetUserById
{
    /// <summary>
    /// Represents the detailed information for a single user, including their roles and all inherited permissions.
    /// </summary>
    public sealed record UserDetailResponse(
        Guid Id,
        string UserCode,
        string Email,
        string FirstName,
        string LastName,
        bool IsActive,
        DateTime CreatedAtUtc,
        List<AssignedRoleResponse> Roles,
        List<PermissionDetailResponse> Permissions);
}
