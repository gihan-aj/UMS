using System;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.RegisterUser
{
    public sealed record RegisterUserCommand(
        string Email,
        string Password,
        string ConfirmPassword,
        string? FirstName,
        string? LastName) : ICommand<Guid>; // Implements ICommand<Guid> returning Result<Guid>
}
