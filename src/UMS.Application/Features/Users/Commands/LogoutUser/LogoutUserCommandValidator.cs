using FluentValidation;

namespace UMS.Application.Features.Users.Commands.LogoutUser
{
    public class LogoutUserCommandValidator : AbstractValidator<LogoutUserCommand>
    {
        public LogoutUserCommandValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }
}
