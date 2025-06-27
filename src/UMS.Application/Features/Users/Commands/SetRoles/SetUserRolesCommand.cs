using System;
using System.Collections.Generic;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.SetRoles
{
    /// <summary>
    /// Command to set (replace) the roles for a specific user.
    /// </summary>
    public sealed record SetUserRolesCommand(
        Guid UserId,
        List<byte> RoleIds) : ICommand;
}
