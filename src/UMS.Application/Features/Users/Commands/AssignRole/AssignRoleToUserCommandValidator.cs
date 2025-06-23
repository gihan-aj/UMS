using FluentValidation;

namespace UMS.Application.Features.Users.Commands.AssignRole
{
    public class AssignRoleToUserCommandValidator : AbstractValidator<AssignRoleToUserCommand>
    {
        public AssignRoleToUserCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.RoleId).NotEmpty();
        }
    }
}
