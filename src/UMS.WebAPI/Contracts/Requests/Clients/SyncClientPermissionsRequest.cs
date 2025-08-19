using System.Collections.Generic;

namespace UMS.WebAPI.Contracts.Requests.Clients
{
    public sealed record SyncPermissionsRequest(List<string> PermissionNames);
}
