using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.Application.Settings;
using UMS.Domain.Users;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.CreateUserByAdmin
{
    public class CreateUserByAdminCommandHandler : ICommandHandler<CreateUserByAdminCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IReferenceCodeGeneratorService _referenceCodeGeneratorService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly TokenSettings _tokenSettings;
        private readonly ILogger<CreateUserByAdminCommandHandler> _logger;

        public CreateUserByAdminCommandHandler(
            IUserRepository userRepository,
            IReferenceCodeGeneratorService referenceCodeGeneratorService,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ILogger<CreateUserByAdminCommandHandler> logger,
            IOptions<TokenSettings> tokenSettings,
            IRoleRepository roleRepository)
        {
            _userRepository = userRepository;
            _referenceCodeGeneratorService = referenceCodeGeneratorService;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _logger = logger;
            _tokenSettings = tokenSettings.Value;
            _roleRepository = roleRepository;
        }

        public async Task<Result<Guid>> Handle(CreateUserByAdminCommand command, CancellationToken cancellationToken)
        {
            var existingUser = await _userRepository.GetByEmailAsync(command.Email);
            if (existingUser is not null)
            {
                return Result.Failure<Guid>(new Error(
                    "User.AlreadyExists",
                    "User with this email already exists.",
                    ErrorType.Conflict));
            }

            var defaultRole = await _roleRepository.GetByNameAsync("User");
            if (defaultRole == null)
            {
                return Result.Failure<Guid>(new Error(
                    "Role.NotFound", 
                    "Default role configuration is missing.", 
                    ErrorType.Failure));
            }

            var createdBy = _currentUserService.UserId;

            var userCode = await _referenceCodeGeneratorService.GenerateReferenceCodeAsync("USR");

            var newUser = User.CreateByAdmin(
                userCode,
                command.Email,
                command.FirstName,
                command.LastName,
                _tokenSettings.ActivationTokenExpiryHours,
                createdBy);

            newUser.AssignRole(defaultRole.Id, createdBy ?? Guid.Empty);

            //foreach(byte roleId in command.RoleIds)
            //{
            //    newUser.AssignRole(roleId, _currentUserService.UserId ?? Guid.Empty);
            //}

            await _userRepository.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} created by admin {AdminId}. Activation email will be sent.", newUser.Id, _currentUserService.UserId);

            return newUser.Id;
        }
    }
}
