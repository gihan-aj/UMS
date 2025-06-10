using FluentValidation;

namespace UMS.Application.Features.Users.Commands.RequestPasswordReset
{
    public class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
    {
        public RequestPasswordResetCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");
        }
    }
}
