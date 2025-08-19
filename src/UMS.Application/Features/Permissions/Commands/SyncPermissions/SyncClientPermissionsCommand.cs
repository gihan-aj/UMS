using System;
using System.Collections.Generic;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Permissions.Commands.SyncPermissions
{
    public sealed record SyncClientPermissionsCommand(
        Guid ClientId, 
        List<string> PermissionNames) : ICommand;
}
