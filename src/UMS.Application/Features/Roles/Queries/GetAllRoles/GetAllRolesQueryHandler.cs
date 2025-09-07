using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Queries;
using UMS.Application.Features.Permissions.Queries.ListPermissions;
using UMS.SharedKernel;

namespace UMS.Application.Features.Roles.Queries.GetAllRoles
{
    public class GetAllRolesQueryHandler : IQueryHandler<GetAllRolesQuery, List<RoleWithDetailedPermissionsResponse>>
    {
        private readonly IRoleRepository _roleRepository;

        public GetAllRolesQueryHandler(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<Result<List<RoleWithDetailedPermissionsResponse>>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = await _roleRepository.GetAllAsync(cancellationToken);
            if(roles is null)
            {
                return new List<RoleWithDetailedPermissionsResponse>();
            }
            else
            {
                return roles
                    .Select(r => 
                    new RoleWithDetailedPermissionsResponse(
                        r.Id, r.Name, 
                        r.Permissions.Select(rp => 
                        new PermissionDetailResponse( 
                            rp.Permission.Name,
                            GenerateDescription(rp.Permission.Name)))
                        .OrderBy(p => p.Name)
                        .ToList()))
                    .ToList();
            }
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
