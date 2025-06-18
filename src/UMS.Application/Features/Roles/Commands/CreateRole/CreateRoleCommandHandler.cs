using Mediator;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Commands;
using UMS.Domain.Authorization;
using UMS.SharedKernel;

namespace UMS.Application.Features.Roles.Commands.CreateRole
{
    public class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, byte>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateRoleCommandHandler> _logger;

        public CreateRoleCommandHandler(
            IRoleRepository roleRepository, 
            IUnitOfWork unitOfWork, 
            ILogger<CreateRoleCommandHandler> logger)
        {
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<byte>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
        {
            // 1. Check if a role with same name already exists
            var existingRole = await _roleRepository.GetByNameAsync(command.Name, cancellationToken);
            if(existingRole is not null)
            {
                _logger.LogWarning("Create role failed: Role with name '{RoleName}' already exists.", command.Name);
                return Result.Failure<byte>(new Error(
                    "Role.AlreadyExists",
                    $"A role with the name '{command.Name}' already exists.",
                    ErrorType.Conflict));
            }

            // 2. Create the new role entity
            // We need a way to generate a new, unique ID. For now, let's assume we can query the max existing ID.
            // A more robust solution might involve a dedicated sequence generator for role IDs.
            var newRoleId = await _roleRepository.GetNextIdAsync();
            var newRole = Role.Create(newRoleId, command.Name);

            // 3. Add the role to the repository
            await _roleRepository.AddAsync(newRole);
            _logger.LogInformation("New role '{RoleName}' with ID {RoleId} marked for addition.", newRole.Name, newRole.Id);

            // 4. Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Domain events would be dispatched here if any were raised.

            return newRole.Id;
        }
    }
}
