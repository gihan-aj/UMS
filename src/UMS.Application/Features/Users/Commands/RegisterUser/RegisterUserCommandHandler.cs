using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.Application.Settings;
using UMS.Domain.Users;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IReferenceCodeGeneratorService _referenceCodeGeneratorService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TokenSettings _tokenSettings;
        private readonly ILogger<RegisterUserCommandHandler> _logger;

        public RegisterUserCommandHandler(
            IPasswordHasherService passwordHasherService,
            IUserRepository userRepository,
            IPublisher publisher,
            ILogger<RegisterUserCommandHandler> logger,
            IReferenceCodeGeneratorService referenceCodeGeneratorService,
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IOptions<TokenSettings> tokenSettings,
            IOptions<ClientAppSettings> clientAppSettings,
            IRoleRepository roleRepository)
        {
            _passwordHasherService = passwordHasherService ?? throw new ArgumentNullException(nameof(userRepository)); ;
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(passwordHasherService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _referenceCodeGeneratorService = referenceCodeGeneratorService ?? throw new ArgumentNullException(nameof(referenceCodeGeneratorService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _tokenSettings = tokenSettings.Value;
            _roleRepository = roleRepository;
        }

        public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            var existingUser = await _userRepository.GetByEmailAsync(command.Email);
            if (existingUser != null)
            {
                return Result.Failure<Guid>(new Error("User.AlreadyExists", "User with this email already exists.", ErrorType.Conflict));
            }

            var defaultRole = await _roleRepository.GetByNameAsync("User");
            if (defaultRole == null)
            {
                return Result.Failure<Guid>(new Error("Role.NotFound", "Default role configuration is missing.", ErrorType.Failure));
            }

            var userCode = await _referenceCodeGeneratorService.GenerateReferenceCodeAsync("USR");
            var passwordHash = _passwordHasherService.HashPassword(command.Password);

            var newUser = User.RegisterNew(
                userCode, 
                command.Email, 
                passwordHash, 
                command.FirstName, 
                command.LastName, 
                _tokenSettings.ActivationTokenExpiryHours,  
                null);

            newUser.AssignRole(defaultRole.Id, Guid.Empty);

            await _userRepository.AddAsync(newUser);

            // The interceptor will handle dispatching events raised by User.Create()
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} created successfully. Domain events will be dispatched.", newUser.Id);

            return newUser.Id;
        }
    }
}
