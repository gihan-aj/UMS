using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Queries;
using UMS.Application.Features.Roles.Queries.GetRoleById;
using UMS.SharedKernel;

namespace UMS.Application.Features.Permissions.Queries.ListPermissions
{
    public class ListPermissionsQueryHandler : IQueryHandler<ListPermissionsQuery, List<PermissionGroupResponse>>
    {
        private readonly IPermissionRepository _permissionRepository;

        public ListPermissionsQueryHandler(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        public async Task<Result<List<PermissionGroupResponse>>> Handle(ListPermissionsQuery request, CancellationToken cancellationToken)
        {
            var response =  new List<PermissionGroupResponse>();

            // 1. Get system properties from the static class
            var systemPermissions = Domain.Authorization.Permissions.GetAllPermissionValues()
                .GroupBy(p => p.Split(":")[0])
                .Select(g => new PermissionGroupResponse(
                    GroupName: $"System: {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(g.Key)}",
                    Permissions: g.Select(p => new PermissionDetailResponse(p, GenerateDescription(p))).ToList()
                ))
                .ToList();

            response.AddRange(systemPermissions);

            // 2. Get Client Permissions from the database
            var clientPermissions = await _permissionRepository.GetClientPermissionsAsync(cancellationToken);
            var allClientPermissions = clientPermissions
                .Where(p => p.Client != null && p.Client.IsDeleted == false)
                .Select(p => new
                {
                    ClientName = p.Client!.ClientName,
                    PermissionName = p.Name
                })
                .ToList();

            // First by Client Name, then by permission resource
            var clientPermissionsGrouped = allClientPermissions
                .GroupBy(p => p.ClientName)
                .SelectMany(clientGroup =>
                {
                    // For each client, group their permissions by the resource part (before the colon)
                    return clientGroup
                        .GroupBy(p => p.PermissionName.Split(':')[0]) // e.g., "orders", "refunds"
                        .Select(resourceGroup => new PermissionGroupResponse(
                            GroupName: $"{clientGroup.Key}: {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(resourceGroup.Key)}", // e.g., "POS System: Orders"
                            Permissions: resourceGroup.Select(p => new PermissionDetailResponse(p.PermissionName, GenerateDescription(p.PermissionName))).ToList()
                        ));
                })
                .ToList();

            response.AddRange(clientPermissionsGrouped);


            return response.OrderBy(g => g.GroupName).ToList();
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
