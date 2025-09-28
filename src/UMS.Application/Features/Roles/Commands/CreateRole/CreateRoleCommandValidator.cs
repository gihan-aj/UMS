using FluentValidation;

namespace UMS.Application.Features.Roles.Commands.CreateRole
{
    public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
    {
        public CreateRoleCommandValidator() 
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Role name is required.")
                .MaximumLength(100).WithMessage("Role name cannot exceed 40 characters.");
            
            RuleFor(x => x.Description)
                .MaximumLength(100).WithMessage("Role description cannot exceed 100 characters.");

            RuleFor(x => x.PermissionNames)
                .NotNull();
        }
    }
}
