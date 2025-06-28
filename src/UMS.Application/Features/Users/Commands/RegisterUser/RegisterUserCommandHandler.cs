using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
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
        private readonly IEmailService _emailService;
        private readonly IPublisher _publisher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TokenSettings _tokenSettings;
        private readonly ClientAppSettings _clientAppSettings;
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
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _referenceCodeGeneratorService = referenceCodeGeneratorService ?? throw new ArgumentNullException(nameof(referenceCodeGeneratorService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _emailService = emailService;
            _tokenSettings = tokenSettings.Value;
            _clientAppSettings = clientAppSettings.Value;
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

            var newUser = User.Create(
                userCode, 
                command.Email, 
                passwordHash, 
                command.FirstName, 
                command.LastName, 
                _tokenSettings.ActivationTokenExpiryHours,  
                null);

            newUser.AssignRole(defaultRole.Id, Guid.Empty);

            await _userRepository.AddAsync(newUser);

            // The interceptor will handle dispatching events raised by User.CreateNew()
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} created successfully. Domain events will be dispatched.", newUser.Id);

            return newUser.Id;
            //_logger.LogInformation("Attempting to register user with email: {Email}", command.Email);

            //bool userExists = await _userRepository.ExistsByEmailAsync(command.Email);
            //if (userExists)
            //{
            //    _logger.LogWarning("User registration failed: Email {Email} already exists.", command.Email);
            //    return Result.Failure<Guid>(new Error("User.AlreadyExists", $"A user with the email '{command.Email}' already exists.", ErrorType.Conflict));
            //}

            //string userCode = await _referenceCodeGeneratorService.GenerateReferenceCodeAsync("USR");

            //string passwordHash = _passwordHasherService.HashPassword(command.Password);

            //Guid? currentUserIdForAudit = null;

            //// --- Assign default Role ---
            //var defaultRole = await _roleRepository.GetByNameAsync("User");
            //if (defaultRole is null)
            //{
            //    _logger.LogError("Default 'User' role not found in the database. Seeding may have failed.");
            //    return Result.Failure<Guid>(new Error("Role.NotFound", "Default role configuration is missing.", ErrorType.Failure));
            //}

            //User newUser;
            //try
            //{
            //    newUser = User.Create(
            //        userCode: userCode,
            //        email: command.Email,
            //        passwordHash: passwordHash,
            //        firstName: command.FirstName,
            //        lastName: command.LastName,
            //        createdByUserId: currentUserIdForAudit
            //    );
            //    _logger.LogInformation("User entity created for email: {Email} with ID: {UserId}, UserCode: {UserCode}, ActivationToken: {ActivationToken}",
            //        newUser.Email, newUser.Id, newUser.UserCode, newUser.ActivationToken);

            //    newUser.AssignRole(defaultRole.Id, Guid.Empty); // Assign role; Guid.Empty for system-assigned

            //    newUser.GenerateActivationToken(_tokenSettings.ActivationTokenExpiryHours);
            //}
            //catch (ArgumentException ex)
            //{
            //    _logger.LogError(ex, "ArgumentException during User.CreateNew for email: {Email}", command.Email);
            //    return Result.Failure<Guid>(new Error("User.CreationError", ex.Message, ErrorType.Validation));
            //}

            //// Add user to the repository (marks for addition in DbContext)
            //await _userRepository.AddAsync(newUser);
            //_logger.LogInformation("User with ID: {UserId} marked for addition.", newUser.Id);

            //// Collect domain events BEFORE saving changes
            //var domainEvents = newUser.GetDomainEvents().ToList();
            //newUser.ClearDomainEvents();

            //// Save changes to the database
            //try
            //{
            //    await _unitOfWork.SaveChangesAsync(cancellationToken);
            //    _logger.LogInformation("User with ID: {UserId} and activation token persisted successfully.", newUser.Id);

            //    // Send activation email AFTER user and token are saved
            //    if (!string.IsNullOrEmpty(newUser.ActivationToken))
            //    {
            //        string activationLink = $"{_clientAppSettings.ActivationLinkBaseUrl}?token={Uri.EscapeDataString(newUser.ActivationToken)}&email={Uri.EscapeDataString(newUser.Email)}";
            //        string emailSubject = "Activate Your UMS Account";
            //        string emailHtmlBody = $"<h1>Welcome to UMS!</h1><p>Please activate your account by clicking the link below:</p><p><a href='{activationLink}'>Activate Account</a></p><p>If you did not request this, please ignore this email.</p><p>Token: {newUser.ActivationToken}</p>"; // Token in body for easy testing

            //        bool emailSent = await _emailService.SendEmailAsync(newUser.Email, emailSubject, emailHtmlBody);
            //        if (emailSent)
            //        {
            //            _logger.LogInformation("Activation email simulation sent to {Email} for user {UserId}.", newUser.Email, newUser.Id);
            //        }
            //        else
            //        {
            //            _logger.LogWarning("Failed to send activation email simulation to {Email} for user {UserId}.", newUser.Email, newUser.Id);
            //            // Decide if this is a critical failure. For now, we proceed.
            //        }
            //    }
            //}
            //catch (DbUpdateException ex)
            //{
            //    _logger.LogError(ex, "DbUpdateException during user persistence for User ID: {UserId}", newUser.Id);
            //    // You might want to inspect inner exceptions for more details (e.g., unique constraint violations not caught earlier)
            //    return Result.Failure<Guid>(new Error("User.PersistenceError.DbUpdate", "A database error occurred while saving the user.", ErrorType.Failure));
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Generic exception during user persistence or email sending for User ID: {UserId}", newUser.Id);
            //    return Result.Failure<Guid>(new Error("User.PersistenceError.Generic", "An unexpected error occurred while saving the user.", ErrorType.Failure));
            //}

            //// Publish domain events AFTER changes have been successfully saved
            //foreach(var domainEvent in domainEvents)
            //{
            //    // The UserCreatedDomainEvent now includes the activation token, which can be used by other handlers if needed.
            //    try
            //    {
            //        await _publisher.Publish(domainEvent, cancellationToken);
            //        _logger.LogInformation("Domain event {DomainEventType} published for User ID: {UserId}", domainEvent.GetType().Name, newUser.Id);
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex, "Exception during publishing domain event {DomainEventType} for User ID: {UserId}. The user was created, but a post-creation event failed.", domainEvent.GetType().Name, newUser.Id);
            //        // Decide if this is critical. Usually, the main operation is considered successful.
            //    }
            //}

            //_logger.LogInformation("User registration successful for User ID: {UserId}, UserCode: {UserCode}", newUser.Id, newUser.UserCode);
            //return Result<Guid>.Success(newUser.Id);
        }
    }
}
