using FluentValidation;

namespace UMS.Application.Features.Roles.Commands.UpdateRole
{
    public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
    {
        public UpdateRoleCommandValidator() 
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Role ID is required.");

            RuleFor(x => x.NewName)
                .NotEmpty().WithMessage("New role name is required.")
                .MaximumLength(100).WithMessage("Role name cannot exceed 40 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(100).WithMessage("Role description cannot exceed 100 characters.");

            RuleFor(x => x.PermissionNames)
                .NotNull();
        }
    }
}
