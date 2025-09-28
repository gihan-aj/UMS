using UMS.Application.Common.Messaging.Queries;

namespace UMS.Application.Features.Roles.Queries.GetRoleById
{
    /// <summary>
    /// Query to get a single role by its ID, including its assigned permissions.
    /// </summary>
    public sealed record GetRoleByIdQuery(byte RoleId) : IQuery<RoleWithPermissionsResponse>;
}
