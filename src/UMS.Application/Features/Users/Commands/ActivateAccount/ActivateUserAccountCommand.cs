using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.ActivateAccount
{
    /// <summary>
    /// Command to activate a user's account using an activation token.
    /// </summary>
    public sealed record ActivateUserAccountCommand(string Email, string Token) : ICommand; // Returns a non-generic result
}
