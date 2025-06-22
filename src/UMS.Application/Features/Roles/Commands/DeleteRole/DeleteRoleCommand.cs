using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.SharedKernel;

namespace UMS.Application.Features.Roles.Commands.DeleteRole
{
    public sealed record DeleteRoleCommand(byte RoleId) : ICommand;

    public class DeleteRoleCommandHandler : ICommandHandler<DeleteRoleCommand>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteRoleCommandHandler> _logger;

        public DeleteRoleCommandHandler(
            IRoleRepository roleRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<DeleteRoleCommandHandler> logger)
        {
            _roleRepository = roleRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(request.RoleId);
            if (role is null)
            {
                return Result.Success();
            }

            if(role.Name is "SuperAdmin" or "User")
            {
                _logger.LogWarning("Attempt to delete system role '{RoleName}' by user {UserId}.", role.Name, _currentUserService.UserId);
                return Result.Failure(new Error("Role.CannotDeleteSystemRole", $"System role '{role.Name}' cannot be deleted.", ErrorType.Conflict));
            }

            bool isRoleInUse = await _roleRepository.IsRoleAssignedToUsersAsync(request.RoleId, cancellationToken);
            if (isRoleInUse)
            {
                _logger.LogWarning("Attempt to delete role '{RoleName}' which is currently assigned to users.", role.Name);
                return Result.Failure(new Error(
                    "Role.InUse",
                    "This role cannot be deleted as it is currently assigned to one or more users. Please reassign users before deleting the role.",
                    ErrorType.Conflict));
            }

            var currentUserId = _currentUserService.UserId;
            role.MarkAsDeleted(currentUserId);
            _logger.LogInformation("Role {RoleId} marked as deleted by user {UserId}.", request.RoleId, currentUserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
