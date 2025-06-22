using Mediator;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.Domain.Authorization;
using UMS.SharedKernel;

namespace UMS.Application.Features.Roles.Commands.CreateRole
{
    public class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, byte>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISequenceGeneratorService _sequenceGeneratorService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateRoleCommandHandler> _logger;

        public CreateRoleCommandHandler(
            IRoleRepository roleRepository,
            IUnitOfWork unitOfWork,
            ILogger<CreateRoleCommandHandler> logger,
            ISequenceGeneratorService sequenceGeneratorService,
            ICurrentUserService currentUserService)
        {
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _sequenceGeneratorService = sequenceGeneratorService;
            _currentUserService = currentUserService;
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
            var newRoleId = await _sequenceGeneratorService.GetNextIdAsync<byte>("Roles", cancellationToken);
            var createdUserId = _currentUserService.UserId;
            var newRole = Role.Create(newRoleId, command.Name, createdUserId);

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
