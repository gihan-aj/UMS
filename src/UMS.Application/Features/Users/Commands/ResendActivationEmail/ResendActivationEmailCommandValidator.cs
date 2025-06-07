using FluentValidation;

namespace UMS.Application.Features.Users.Commands.ResendActivationEmail
{
    public class ResendActivationEmailCommandValidator : AbstractValidator<ResendActivationEmailCommand>
    {
        public ResendActivationEmailCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");
        }
    }
}
