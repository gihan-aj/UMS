using FluentValidation;

namespace UMS.Application.Features.Roles.Commands.AssignPermissions
{
    public class AssignPermissionsToRoleCommandValidator : AbstractValidator<AssignPermissionsToRoleCommand>
    {
        public AssignPermissionsToRoleCommandValidator()
        {
            RuleFor(x => x.RoleId).NotEmpty();
            RuleFor(x => x.PermissionNames).NotNull();
        }
    }
}
