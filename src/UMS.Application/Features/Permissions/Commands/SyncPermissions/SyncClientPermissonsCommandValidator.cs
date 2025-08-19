using FluentValidation;

namespace UMS.Application.Features.Permissions.Commands.SyncPermissions
{
    public class SyncClientPermissonsCommandValidator : AbstractValidator<SyncClientPermissionsCommand>
    {
        public SyncClientPermissonsCommandValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty();
            RuleFor(x => x.PermissionNames).NotNull();
        }
    }
}
