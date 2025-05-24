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
        private readonly IPublisher _publisher; // Inject IPublisher
        private readonly ILogger<RegisterUserCommandHandler> _logger;


        public RegisterUserCommandHandler(
            IPasswordHasherService passwordHasherService, 
            IUserRepository userRepository, 
            IPublisher publisher,
            ILogger<RegisterUserCommandHandler> logger)
        {
            _passwordHasherService = passwordHasherService ?? throw new ArgumentNullException(nameof(userRepository)); ;
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(passwordHasherService));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher)); // Initialize IPublisher
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            // 2. Hash the password
            string passwordHash = _passwordHasherService.HashPassword(command.Password);
            _logger.LogInformation("Password hashed for email: {Email}", command.Email);

            // 3. Create the User domain entity
            User newUser;
            try
            {
                newUser = User.Create(
                    email: command.Email,
                    passwordHash: passwordHash,
                    firstName: command.FirstName,
                    lastName: command.LastName
                );
                _logger.LogInformation("User entity created for email: {Email} with ID: {UserId}", newUser.Email, newUser.Id);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "ArgumentException during User.Create for email: {Email}", command.Email);
                return Result.Failure<Guid>(new Error("User.CreationError", ex.Message, ErrorType.Validation));
            }

            // 4. Persist the user
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

            // 5. Publish domain event (UserCreatedNotification)
            try
            {
                var userCreatedNotification = new UserCreatedNotification(newUser.Id, newUser.Email);
                await _publisher.Publish(userCreatedNotification, cancellationToken); // Use the generic overload
                _logger.LogInformation("UserCreatedNotification published for User ID: {UserId}", newUser.Id);
            }
            catch (Exception ex)
            {
                // Log the error but don't let notification failure fail the entire command.
                // This depends on your business requirements. Sometimes, notification failures
                // might be critical, other times they are secondary.
                _logger.LogError(ex, "Exception during publishing UserCreatedNotification for User ID: {UserId}", newUser.Id);
                // Optionally, you could collect these errors or handle them differently.
            }

            // 6. Return success with the new User's ID
            _logger.LogInformation("User registration successful for User ID: {UserId}", newUser.Id);
            return Result<Guid>.Success(newUser.Id);
        }
    }
}
