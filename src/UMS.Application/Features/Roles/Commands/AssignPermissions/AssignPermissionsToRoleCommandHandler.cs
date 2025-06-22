using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.Domain.Authorization;
using UMS.SharedKernel;

namespace UMS.Application.Features.Roles.Commands.AssignPermissions
{
    public class AssignPermissionsToRoleCommandHandler : ICommandHandler<AssignPermissionsToRoleCommand>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignPermissionsToRoleCommandHandler> _logger;

        public AssignPermissionsToRoleCommandHandler(
            IRoleRepository roleRepository, 
            ICurrentUserService currentUserService, 
            IUnitOfWork unitOfWork, 
            ILogger<AssignPermissionsToRoleCommandHandler> logger)
        {
            _roleRepository = roleRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(AssignPermissionsToRoleCommand command, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdWithPermissionsAsync(command.RoleId, cancellationToken);
            if(role is null)
            {
                return Result.Failure(new Error(
                    "Role.NotFound",
                    $"Role with ID {command.RoleId} not found.",
                    ErrorType.NotFound));
            }

            if(role.Name is "SuperAdmin")
            {
                return Result.Failure(new Error(
                    "Role.CannotModifySuperAdmin",
                    "The SuperAdmin role's permissions cannot be modified.",
                    ErrorType.Conflict));
            }

            // Get the current set of permission ids assigned to the role
            var currentPermissionIds = role.Permissions.Select(rp => rp.PermissionId).ToHashSet();

            // Get the requested set of permission ids
            var requestedPermssionIds = command.PermissionIds.ToHashSet();

            // Find permssions to add
            var permissionsToAdd = requestedPermssionIds.Except(currentPermissionIds).ToList();

            // Find permissions to remove
            var permissionsToRemove = currentPermissionIds.Except(requestedPermssionIds).ToList();

            // Remove old permissions
            if(permissionsToRemove.Any())
            {
                var rolePermissionsToRemove = role.Permissions
                    .Where(rp => permissionsToRemove.Contains(rp.PermissionId))
                    .ToList();

                _roleRepository.RemoveRolePermissionsRange(rolePermissionsToRemove);
                _logger.LogInformation("Removing {Count} permissions from role {RoleId}.", rolePermissionsToRemove.Count, role.Id);
            }

            // Add new permissions
            if (permissionsToAdd.Any())
            {
                var existingPermissionIds = await _roleRepository.GetExistingPermissionsAsync(permissionsToAdd, cancellationToken);

                var newRolePermissions = existingPermissionIds
                    .Select(permissionId => new RolePermission { RoleId = command.RoleId, PermissionId = permissionId })
                    .ToList();

                await _roleRepository.AddRolePermissionsRangeAsync(newRolePermissions,cancellationToken);
                _logger.LogInformation("Adding {Count} permissions to role {RoleId}.", newRolePermissions.Count, role.Id);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
