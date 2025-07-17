using System;
using System.Collections.Generic;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.CreateUserByAdmin
{
    /// <summary>
    /// Command for an admin to create a new user account
    /// </summary>
    /// <param name="Email"></param>
    /// <param name="FirstName"></param>
    /// <param name="LastName"></param>
    /// <param name="RoleIds"></param>
    public sealed record CreateUserByAdminCommand(
        string Email,
        string FirstName,
        string LastName,
        List<byte> RoleIds) : ICommand<Guid>;
}
