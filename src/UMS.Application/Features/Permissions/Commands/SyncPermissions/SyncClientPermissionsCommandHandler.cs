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

            var permissionNamesToAdd = requestedPermissionNames.Except(existingPermissionNames);
            if(permissionNamesToAdd.Any())
            {
                var lastId = await _permissionRepository.GetTheLastId(cancellationToken);
                var permissionsToAdd = permissionNamesToAdd
                    .Select(name => Permission.Create(++lastId, name, command.ClientId))
                    .ToList();

                await _permissionRepository.AddRangeAsync(permissionsToAdd, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

            }

            return Result.Success();
        }
    }
}
