using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Commands;
using UMS.Domain.Authorization;
using UMS.SharedKernel;

namespace UMS.Application.Features.Permissions.Commands.SyncPermissions
{
    public class SyncClientPermissionsCommandHandler : ICommandHandler<SyncClientPermissionsCommand>
    {
        private readonly IClientRepository _clientRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SyncClientPermissionsCommandHandler> _logger;

        public SyncClientPermissionsCommandHandler(
            IClientRepository clientRepository,
            IPermissionRepository permissionRepository,
            ILogger<SyncClientPermissionsCommandHandler> logger,
            IUnitOfWork unitOfWork)
        {
            _clientRepository = clientRepository;
            _permissionRepository = permissionRepository;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(SyncClientPermissionsCommand command, CancellationToken cancellationToken)
        {
            var client = await _clientRepository.GetByIdAsync(command.ClientId, cancellationToken);
            if (client is null)
            {
                return Result.Failure(
                    new Error(
                        "Client.NotFound", 
                        "The specified client was not found.", 
                        ErrorType.NotFound));

            }

            var existingPermissions = await _permissionRepository.GetPermissionsByClientIdAsync(command.ClientId, cancellationToken);
            var existingPermissionNames = existingPermissions.Select(p => p.Name).ToHashSet();
            var requestedPermissionNames = command.PermissionNames.ToHashSet();

            // --- Calculate permissions to ADD ---
            var namesToAdd = requestedPermissionNames.Except(existingPermissionNames).ToList();
            if (namesToAdd.Any())
            {
                var permissionsToAdd = namesToAdd
                    .Select(name => Permission.Create(0, name, client.Id))
                    .ToList();
                await _permissionRepository.AddRangeAsync(permissionsToAdd, cancellationToken);
            }

            // --- Calculate permissions to REMOVE ---
            var namesToRemove = existingPermissionNames.Except(requestedPermissionNames).ToList();
            if (namesToRemove.Any())
            {
                var permissionsToRemove = existingPermissions.Where(p => namesToRemove.Contains(p.Name)).ToList();
                var permissionIdsToRemove = permissionsToRemove.Select(p => p.Id).ToList();

                // SAFETY CHECK: Ensure these permissions are not currently assigned to any roles.
                if(await _permissionRepository.IsAnyPermissionsInUse(permissionIdsToRemove))
                {
                    var inUsePermissionIds = await _permissionRepository.GetInUsePermissionIds(permissionIdsToRemove, cancellationToken);
                    var inUsePermissions = await _permissionRepository.GetPermissionsByIdsAsync(inUsePermissionIds, cancellationToken);
                    var inUsePermissionNames = string.Join(", ", inUsePermissions.Select(p => p.Name));

                    return Result.Failure(new Error(
                        "Permission.InUse",
                        $"Cannot remove permissions that are currently assigned to roles. Please unassign the following permissions first: {inUsePermissionNames}",
                        ErrorType.Conflict));
                }

                _permissionRepository.RemoveRange(permissionsToRemove);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
