using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Users.Commands.UpdateMyProfile
{
    public sealed record UpdateMyProfileCommand(
        string FirstName,
        string LastName) : ICommand;
}
