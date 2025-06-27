using FluentValidation;

namespace UMS.Application.Features.Users.Commands.SetRoles
{
    public class SetUserRolesCommandValidator : AbstractValidator<SetUserRolesCommand>
    {
        public SetUserRolesCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.RoleIds).NotNull();
        }
    }
}
