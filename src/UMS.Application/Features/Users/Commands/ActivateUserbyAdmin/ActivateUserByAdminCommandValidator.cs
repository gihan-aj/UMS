using FluentValidation;

namespace UMS.Application.Features.Users.Commands.ActivateUserbyAdmin
{
    public class ActivateUserByAdminCommandValidator : AbstractValidator<ActivateUserByAdminCommand>
    {
        public ActivateUserByAdminCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}
