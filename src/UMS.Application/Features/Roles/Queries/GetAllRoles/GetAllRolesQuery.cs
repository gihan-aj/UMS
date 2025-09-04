using System.Collections.Generic;
using UMS.Application.Common.Messaging.Queries;
using UMS.Application.Features.Roles.Queries.ListQueries;

namespace UMS.Application.Features.Roles.Queries.GetAllRoles
{
    public sealed record GetAllRolesQuery(): IQuery<List<RoleResponse>>;
}
