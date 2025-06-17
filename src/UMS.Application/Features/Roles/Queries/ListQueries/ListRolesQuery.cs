using UMS.Application.Common.Messaging.Queries;
using UMS.SharedKernel;

namespace UMS.Application.Features.Roles.Queries.ListQueries
{
    /// <summary>
    /// Query to get a paginated list of all roles.
    /// </summary>
    public sealed record ListRolesQuery(
        int Page,
        int PageSize,
        string? SearchTerm) : IQuery<PagedList<RoleResponse>>;
}
