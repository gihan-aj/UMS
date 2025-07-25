using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.Domain.Authorization;
using UMS.SharedKernel;

namespace UMS.Application.Features.Roles.Commands.UpdateRole
{
    public class UpdateRoleCommandHandler : ICommandHandler<UpdateRoleCommand>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateRoleCommandHandler> _logger;

        public UpdateRoleCommandHandler(
            IRoleRepository roleRepository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateRoleCommandHandler> logger,
            ICurrentUserService currentUserService,
            IPermissionRepository permissionRepository)
        {
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUserService = currentUserService;
            _permissionRepository = permissionRepository;
        }

        public async Task<Result> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdWithPermissionsAsync(command.RoleId);
            if (role is null)
            {
                return Result.Failure(new Error(
                    "Role.NotFound",
                    $"Role with id {command.RoleId} not found.",
                    ErrorType.NotFound));
            }

            // Check if another role already has the new name
            if(!role.Name.Equals(command.NewName, StringComparison.OrdinalIgnoreCase))
            {
                var existingRoleWithNewName = await _roleRepository.GetByNameAsync(command.NewName, cancellationToken);
                if(existingRoleWithNewName is not null)
                {
                    return Result.Failure(new Error(
                        "Role.AlreadyExists",
                        $"A role with the name '{command.NewName}' already exists.",
                        ErrorType.Conflict));
                }
            }

            if (role.Name is "SuperAdmin" or "User")
            {
                _logger.LogWarning("Attempt to update system role '{RoleName}' by user {UserId}.", role.Name, _currentUserService.UserId);
                return Result.Failure(new Error("Role.CannotUpdateSystemRole", $"System role '{role.Name}' cannot be updated.", ErrorType.Conflict));
            }

            role.UpdateName(command.NewName, _currentUserService.UserId);

            // Get the requested set of permissions
            var requestedPermissions = await _permissionRepository.GetPermissionsByNameRangeAsync(command.PermissionNames, cancellationToken);
            var requestedPermssionIds = requestedPermissions
                .Select(permission => permission.Id)
                .ToHashSet();

            // Get the current set of permission ids assigned to the role
            var currentPermissionIds = role.Permissions.Select(rp => rp.PermissionId).ToHashSet();

            // Find permssions to add
            var permissionsIdsToAdd = requestedPermssionIds.Except(currentPermissionIds).ToList();

            // Find permissions to remove
            var permissionsIdsToRemove = currentPermissionIds.Except(requestedPermssionIds).ToList();

            // Remove old permissions
            if (permissionsIdsToRemove.Any())
            {
                var rolePermissionsToRemove = role.Permissions
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

                await _roleRepository.AddRolePermissionsRangeAsync(newRolePermissions, cancellationToken);
                _logger.LogInformation("Adding {Count} permissions to role {RoleId}.", newRolePermissions.Count, role.Id);
            }

            _logger.LogInformation("Updating role {RoleId} name to '{NewName}'.", command.RoleId, command.NewName);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
