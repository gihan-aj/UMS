using System.Collections.Generic;
using UMS.Application.Common.Messaging.Queries;

namespace UMS.Application.Features.Roles.Queries.GetAllRoles
{
    public sealed record GetAllRolesQuery(): IQuery<List<RoleWithDetailedPermissionsResponse>>;
}
