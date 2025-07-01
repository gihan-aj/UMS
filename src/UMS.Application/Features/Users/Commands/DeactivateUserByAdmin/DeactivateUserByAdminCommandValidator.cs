using FluentValidation;

namespace UMS.Application.Features.Users.Commands.DeactivateUserByAdmin
{
    public class DeactivateUserByAdminCommandValidator : AbstractValidator<DeactivateUserByAdminCommand>
    {
        public DeactivateUserByAdminCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}
