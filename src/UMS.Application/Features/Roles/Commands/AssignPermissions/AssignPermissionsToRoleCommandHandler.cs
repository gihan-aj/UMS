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
        private readonly IPermissionRepository _permissionRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignPermissionsToRoleCommandHandler> _logger;

        public AssignPermissionsToRoleCommandHandler(
            IRoleRepository roleRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<AssignPermissionsToRoleCommandHandler> logger,
            IPermissionRepository permissionRepository)
        {
            _roleRepository = roleRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _permissionRepository = permissionRepository;
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

            // --- SECURITY CHECK ---
            if (_currentUserService.RoleNames.Contains(role.Name))
            {
                _logger.LogWarning("Security violation: User {UserId} attempted to change permissions on their own role '{RoleName}'.", _currentUserService.UserId, role.Name);
                return Result.Failure(new Error(
                    "Role.CannotModifyOwnRole", 
                    "You cannot change permissions on a role that you are currently assigned to.", 
                    ErrorType.Forbidden));
            }

            if (role.Name is "SuperAdmin")
            {
                return Result.Failure(new Error(
                    "Role.CannotModifySuperAdmin",
                    "The SuperAdmin role's permissions cannot be modified.",
                    ErrorType.Conflict));
            }

            if (role.Name is "User")
            {
                return Result.Failure(new Error(
                    "Role.CannotModifyDefaultUser",
                    "The Default user role's permissions cannot be modified.",
                    ErrorType.Conflict));
            }

            // Get the requested set of permissions
            var requestedPermissions = await _permissionRepository.GetPermissionsByNameRangeAsync(command.PermissionNames, cancellationToken);
            var requestedPermssionIds = requestedPermissions
                .Select(permission => permission.Id)
                .ToHashSet();

            // Get the current set of permission ids assigned to the role
            var currentPermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

            // Find permssions to add
            var permissionsIdsToAdd = requestedPermssionIds.Except(currentPermissionIds).ToList();

            // Find permissions to remove
            var permissionsIdsToRemove = currentPermissionIds.Except(requestedPermssionIds).ToList();

            // Remove old permissions
            if(permissionsIdsToRemove.Any())
            {
                var rolePermissionsToRemove = role.RolePermissions
                    .Where(rp => permissionsIdsToRemove.Contains(rp.PermissionId))
                    .ToList();

                _roleRepository.RemoveRolePermissionsRange(rolePermissionsToRemove);
                _logger.LogInformation("Removing {Count} permissions from role {RoleId}.", rolePermissionsToRemove.Count, role.Id);
            }

            // Add new permissions
            if (permissionsIdsToAdd.Any())
            {
                var newRolePermissions = permissionsIdsToAdd
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
