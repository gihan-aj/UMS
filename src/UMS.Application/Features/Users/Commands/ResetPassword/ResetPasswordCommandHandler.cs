using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.SharedKernel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace UMS.Application.Features.Users.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IPublisher _publisher;
        private readonly ILogger<ResetPasswordCommandHandler> _logger;

        public ResetPasswordCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IPasswordHasherService passwordHasherService,
            IPublisher publisher,
            ILogger<ResetPasswordCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _passwordHasherService = passwordHasherService;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to reset password for email: {Email}", command.Email);

            var user = await _userRepository.GetByEmailAsync(command.Email);
            if(user is null)
            {
                _logger.LogWarning("Password reset failed: User not found for email {Email}.", command.Email);
                // Return a generic error to prevent confirming that a user exists.
                return Result.Failure(new Error(
                    "User.InvalidCredentials",
                    "The email or the token provided is invalid.",
                    ErrorType.Validation));
            }

            try
            {
                string newPasswordHash = _passwordHasherService.HashPassword(command.NewPassword);
                user.ResetPassword(newPasswordHash, command.Token);
                _logger.LogInformation("Password has been reset in domain entity for user {UserId}", user.Id);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Password reset failed for user {UserId} due to invalid domain operation.", user.Id);
                return Result.Failure(new Error("User.ResetFailed", ex.Message, ErrorType.Validation));
            }

            var domainEvents = user.GetDomainEvents().ToList();
            user.ClearDomainEvents();

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Password reset for user {UserId} persisted successfully.", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save password reset changes for user {UserId}", user.Id);
                return Result.Failure(new Error("User.PersistenceError", "Failed to save new password.", ErrorType.Failure));
            }

            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }

            return Result.Success();
        }
    }
}
