using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.Domain.Users;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IReferenceCodeGeneratorService _referenceCodeGeneratorService;
        private readonly IPublisher _publisher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RegisterUserCommandHandler> _logger;


        public RegisterUserCommandHandler(
            IPasswordHasherService passwordHasherService,
            IUserRepository userRepository,
            IPublisher publisher,
            ILogger<RegisterUserCommandHandler> logger,
            IReferenceCodeGeneratorService referenceCodeGeneratorService,
            IUnitOfWork unitOfWork)
        {
            _passwordHasherService = passwordHasherService ?? throw new ArgumentNullException(nameof(userRepository)); ;
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(passwordHasherService));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _referenceCodeGeneratorService = referenceCodeGeneratorService ?? throw new ArgumentNullException(nameof(referenceCodeGeneratorService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            bool userExists = await _userRepository.ExistsByEmailAsync(command.Email);
            if (userExists)
            {
                return Result.Failure<Guid>(new Error("User.AlreadyExists", $"A user with the email '{command.Email}' already exists.", ErrorType.Conflict));
            }

            string userCode = await _referenceCodeGeneratorService.GenerateReferenceCodeAsync("USR");

            string passwordHash = _passwordHasherService.HashPassword(command.Password);

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
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<Guid>(new Error("User.CreationError", ex.Message, ErrorType.Validation));
            }

            // Add user to the repository (marks for addition in DbContext)
            await _userRepository.AddAsync(newUser);

            // Collect domain events BEFORE saving changes
            var domainEvents = newUser.GetDomainEvents().ToList();
            newUser.ClearDomainEvent();

            // Save changes to the database
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // You might want to inspect inner exceptions for more details (e.g., unique constraint violations not caught earlier)
                return Result.Failure<Guid>(new Error("User.PersistenceError.DbUpdate", "A database error occurred while saving the user.", ErrorType.Failure));
            }
            catch (Exception)
            {
                return Result.Failure<Guid>(new Error("User.PersistenceError.Generic", "An unexpected error occurred while saving the user.", ErrorType.Failure));
            }

            // Publish domain events AFTER changes have been successfully saved
            foreach(var domainEvent in domainEvents)
            {
                try
                {
                    await _publisher.Publish(domainEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception during publishing domain event {DomainEventType} for User ID: {UserId}. The user was created, but a post-creation event failed.", domainEvent.GetType().Name, newUser.Id);
                    // Decide if this is critical. Usually, the main operation is considered successful.
                }
            }

            _logger.LogInformation("User registration successful for User ID: {UserId}, UserCode: {UserCode}", newUser.Id, newUser.UserCode);
            return Result<Guid>.Success(newUser.Id);
        }
    }
}
