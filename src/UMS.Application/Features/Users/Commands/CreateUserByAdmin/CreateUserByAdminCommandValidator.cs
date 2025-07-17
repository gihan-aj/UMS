using FluentValidation;

namespace UMS.Application.Features.Users.Commands.CreateUserByAdmin
{
    public class CreateUserByAdminCommandValidator : AbstractValidator<CreateUserByAdminCommand>
    {
        public CreateUserByAdminCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.")
                .MaximumLength(255).WithMessage("Email cannot exceed 255 characters.");

            RuleFor(x => x.FirstName)
                .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

            RuleFor(x => x.LastName)
                .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");
        }
    }
}
