using FluentValidation;

namespace UMS.Application.Features.Users.Commands.ActivateAccount
{
    public class ActivateUserAccountCommandValidator : AbstractValidator<ActivateUserAccountCommand>
    {
        public ActivateUserAccountCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Activation token is required.");
        }
    }
}
