using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Commands;
using UMS.SharedKernel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace UMS.Application.Features.Users.Commands.ActivateAccount
{
    public class ActivateUserAccountCommandHandler : ICommandHandler<ActivateUserAccountCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublisher _publisher;
        private readonly ILogger<ActivateUserAccountCommandHandler> _logger;

        public ActivateUserAccountCommandHandler(
            IUserRepository userRepository, 
            IUnitOfWork unitOfWork, 
            IPublisher publisher, 
            ILogger<ActivateUserAccountCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<Result> Handle(ActivateUserAccountCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to activate account for email: {Email} with token: {Token}", command.Email, command.Token);

            // 1. Retrieve the user by email.
            // We need to include soft-deleted users here if activation should be possible for a recently "soft-deleted then restored" scenario.
            // However, our User.Activate() method throws if IsDeleted is true.
            // And GetByEmailAsync by default filters out deleted users.
            // If a user somehow has an activation token but is soft-deleted, they shouldn't be able to activate.
            var user = await _userRepository.GetByEmailAsync(command.Email);
            if(user is null)
            {
                _logger.LogWarning("Account activation failed: User not found for email {Email}", command.Email);
                return Result.Failure(new Error(
                    "Activation.UserNotFound",
                    "User not found or token is invalid.",
                    ErrorType.NotFound));
            }

            // 2. Validate the activation token using the domain entity's logic.
            if(!user.ValidateActivationToken(command.Token))
            {
                _logger.LogWarning("Account activation failed: Invalid or expired token for user {UserId}, email {Email}.", user.Id, command.Email);
                return Result.Failure(new Error(
                    "Activation.InvalidToken",
                    "The activation token is invalid or expired.",
                    ErrorType.Validation)); // Or Unauthorized/Forbidden
            }

            // 3. Check if already active.
            if (user.IsActive)
            {
                _logger.LogInformation("Account for user {UserId}, email {Email} is already active.", user.Id, command.Email);
                return Result.Success(); // Idempotent: already active is a success.
            }

            // 4. Activate the user account (domain method).
            // The User.Activate() method also clears the token and raises UserAccountActivatedDomainEvent.
            try
            {
                // For modifiedByUserId, this is an action initiated by the user themselves via the token.
                // We can pass user.Id or null if a separate "system" actor is implied.
                user.Activate(user.Id);
                _logger.LogInformation("User account {UserId} marked as active in domain entity.", user.Id);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Account activation failed for user {UserId}: {ErrorMessage}", user.Id, ex.Message);
                return Result.Failure(new Error(
                    "Activation.InvalidState",
                    ex.Message,
                    ErrorType.Conflict));
            }

            // The User entity itself would have its LastModifiedAtUtc and LastModifiedBy updated by user.Activate()
            // No need to explicitly call _userRepository.UpdateAsync(user) if using EF Core change tracking.
            // The UnitOfWork will save the changes made to the tracked 'user' entity.

            // 5. Save changes to the database.
            var domainEvents = user.GetDomainEvents().ToList();
            user.ClearDomainEvents();

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("User account {UserId} successfully activated and changes persisted.", user.Id);
            }
            catch(DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException during account activation for User ID: {UserId}", user.Id);
                return Result.Failure(new Error(
                    "Activation.PersistenceError.DbUpdate", 
                    "A database error occurred while activating the account.", 
                    ErrorType.Failure));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic exception during account activation for User ID: {UserId}", user.Id);
                return Result.Failure(new Error(
                    "Activation.PersistenceError.Generic", 
                    "An unexpected error occurred while activating the account.", 
                    ErrorType.Failure));
            }

            // 6. Publish domain events (UserAccountActivatedDomainEvent).
            foreach (var domainEvent in domainEvents)
            {
                try
                {
                    await _publisher.Publish(domainEvent, cancellationToken);
                    _logger.LogInformation("Domain event {DomainEventType} published for User ID: {UserId} after activation.", domainEvent.GetType().Name, user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception during publishing domain event {DomainEventType} for User ID: {UserId} after activation.", domainEvent.GetType().Name, user.Id);
                    // Log and continue; activation itself was successful.
                }
            }

            return Result.Success();
        }
    }
}
