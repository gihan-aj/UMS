using UMS.Application.Common.Messaging.Queries;
using UMS.SharedKernel;

namespace UMS.Application.Features.Roles.Queries.ListRoles
{
    /// <summary>
    /// Query to get a paginated list of all roles.
    /// </summary>
    public sealed record ListRolesQuery(PaginationQuery Query) : IQuery<PagedList<RoleResponse>>;
}
