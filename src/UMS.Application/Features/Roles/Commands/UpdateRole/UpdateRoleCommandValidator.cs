using FluentValidation;

namespace UMS.Application.Features.Roles.Commands.UpdateRole
{
    public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
    {
        public UpdateRoleCommandValidator() 
        {
            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("Role ID is required.");

            RuleFor(x => x.NewName)
                .NotEmpty().WithMessage("New role name is required.")
                .MaximumLength(100).WithMessage("Role name cannot exceed 100 characters.");

            RuleFor(x => x.PermissionNames)
                .NotNull();
        }
    }
}
