using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Common.Messaging.Queries;
using UMS.SharedKernel;

namespace UMS.Application.Features.Permissions.Queries.ListPermissions
{
    public class ListPermissionsQueryHandler : IQueryHandler<ListPermissionsQuery, List<PermissionGroupResponse>>
    {
        public Task<Result<List<PermissionGroupResponse>>> Handle(ListPermissionsQuery request, CancellationToken cancellationToken)
        {
            // Get all permission strings from the static source of truth.
            var allPermissions = Domain.Authorization.Permissions.GetAllPermissionValues();

            // Group permissions by the feature name
            var groupedPermissions = allPermissions
                .GroupBy(p => p.Split(':')[0])
                .Select(g => new PermissionGroupResponse(
                    GroupName: CultureInfo.CurrentCulture.TextInfo.ToTitleCase(g.Key),
                    Permissions: g.Select(p => new PermissionDetailsResponse(
                        Name: p,
                        Description: GenerateDescription(p)
                    )).ToList()
                ))
                .OrderBy(g => g.GroupName)
                .ToList();

            return Task.FromResult(Result<List<PermissionGroupResponse>>.Success(groupedPermissions));
        }

        private static string GenerateDescription(string permissionName)
        {
            // "users:read" -> "Read Users"
            // "roles:assign_permissions" -> "Assign Permissions Roles" (we can refine this)
            var parts = permissionName.Split(':');
            if (parts.Length != 2) return permissionName;

            var action = parts[1].Replace("_", " ");
            var resource = parts[0];

            // A simple transformation to make it more readable
            return $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(action)} {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(resource)}";
        }
    }
}
