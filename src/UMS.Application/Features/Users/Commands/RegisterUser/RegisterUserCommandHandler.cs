using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.Application.Features.Users.Notifications;
using UMS.Domain.Users;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IReferenceCodeGeneratorService _referenceCodeGeneratorService;
        private readonly IPublisher _publisher; // Inject IPublisher
        private readonly ILogger<RegisterUserCommandHandler> _logger;


        public RegisterUserCommandHandler(
            IPasswordHasherService passwordHasherService,
            IUserRepository userRepository,
            IPublisher publisher,
            ILogger<RegisterUserCommandHandler> logger,
            IReferenceCodeGeneratorService referenceCodeGeneratorService)
        {
            _passwordHasherService = passwordHasherService ?? throw new ArgumentNullException(nameof(userRepository)); ;
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(passwordHasherService));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher)); // Initialize IPublisher
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _referenceCodeGeneratorService = referenceCodeGeneratorService ?? throw new ArgumentNullException(nameof(referenceCodeGeneratorService));
        }

        public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to register user with email: {Email}", command.Email);

            // 1. Check if user already exists
            bool userExists = await _userRepository.ExistsByEmailAsync(command.Email.ToLowerInvariant());
            if (userExists)
            {
                _logger.LogWarning("User registration failed: Email {Email} already exists.", command.Email);
                return Result.Failure<Guid>(new Error(
                    "User.AlreadyExists",
                    $"A user with the email '{command.Email}' already exists.",
                    ErrorType.Conflict));
            }
            _logger.LogInformation("Email {Email} is available for registration.", command.Email);

            // 2. Generate UserCode
            string userCode = await _referenceCodeGeneratorService.GenerateReferenceCodeAsync("USR");
            _logger.LogInformation("Generated UserCode: {UserCode} for email: {Email}", userCode, command.Email);

            // 3. Hash the password
            string passwordHash = _passwordHasherService.HashPassword(command.Password);
            _logger.LogInformation("Password hashed for email: {Email}", command.Email);

            // 4. Create the User domain entity
            // For createdByUserId, you'd typically get this from an ICurrentUserService or similar.
            // Passing null for now as we haven't implemented that.
            Guid? currentUserIdForAudit = null;
            User newUser;
            try
            {
                newUser = User.Create(
                    userCode: userCode,
                    email: command.Email,
                    passwordHash: passwordHash,
                    firstName: command.FirstName,
                    lastName: command.LastName,
                    createdByUserId: currentUserIdForAudit
                );
                _logger.LogInformation("User entity created for email: {Email} with ID: {UserId} and UserCode: {UserCode}", newUser.Email, newUser.Id, newUser.UserCode);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "ArgumentException during User.Create for email: {Email}", command.Email);
                return Result.Failure<Guid>(new Error("User.CreationError", ex.Message, ErrorType.Validation));
            }

            // 5. Persist the user
            try
            {
                await _userRepository.AddAsync(newUser);
                _logger.LogInformation("User with ID: {UserId} persisted successfully.", newUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during user persistence for User ID: {UserId}", newUser.Id);
                return Result.Failure<Guid>(new Error(
                    "User.PersistenceError",
                    "An error occurred while saving the user.",
                    ErrorType.Failure));
            }

            // 6. Publish domain event (UserCreatedNotification)
            // The actual dispatching of these events needs to be handled, typically after SaveChangesAsync.
            // For now, we assume they are collected. We'll address dispatching later.
            // Example of how you might manually publish if not using an automated dispatcher:
            foreach(var domainEvent in newUser.GetDomainEvents())
            {
                try
                {
                    // If INotification is from UMS.Mediator, IPublisher is from UMS.Mediator
                    // If INotification is from UMS.Application.Common.Messaging, IPublisher should be compatible.
                    await _publisher.Publish(domainEvent, cancellationToken);
                    _logger.LogInformation("Domain event {DomainEventType} published for User ID: {UserId}", domainEvent.GetType().Name, newUser.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception during publishing domain event {DomainEventType} for User ID: {UserId}", domainEvent.GetType().Name, newUser.Id);
                }
            }
            newUser.ClearDomainEvent(); // Clear events after attemting to publish

            // 7. Return success with the new User's ID
            _logger.LogInformation("User registration successful for User ID: {UserId}, UserCode: {UserCode}", newUser.Id, newUser.UserCode);
            return Result<Guid>.Success(newUser.Id);
        }
    }
}
