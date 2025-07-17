using FluentValidation;

namespace UMS.Application.Features.Users.Commands.SetInitialPassword
{
    public class SetInitialPasswordCommandValidator : AbstractValidator<SetInitialPasswordCommand>
    {
        public SetInitialPasswordCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.")
                .MaximumLength(255).WithMessage("Email cannot exceed 255 characters.");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                // Add more complexity rules if needed (e.g., regex for uppercase, number, symbol)
                .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Password confirmation is required.")
                .Equal(x => x.NewPassword).WithMessage("Password and confirmation password do not match.");
        }
    }
}
