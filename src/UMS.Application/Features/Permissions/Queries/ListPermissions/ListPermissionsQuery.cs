using System.Collections.Generic;
using System.Linq;
using UMS.Application.Common.Messaging.Queries;

namespace UMS.Application.Features.Permissions.Queries.ListPermissions
{
    public sealed record ListPermissionsQuery() : IQuery<List<PermissionGroupResponse>>;
}
