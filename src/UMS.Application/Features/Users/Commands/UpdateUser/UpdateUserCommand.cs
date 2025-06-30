using Mediator;
using System;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.UpdateUser
{
    /// <summary>
    /// Command for an administrator to update a user's profile information.
    /// </summary>
    public sealed record UpdateUserCommand(
        Guid UserId,
        string FirstName,
        string LastName) : ICommand;
}
