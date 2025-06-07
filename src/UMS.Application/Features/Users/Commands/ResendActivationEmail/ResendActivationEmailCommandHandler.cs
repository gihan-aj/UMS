using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.SharedKernel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace UMS.Application.Features.Users.Commands.ResendActivationEmail
{
    public class ResendActivationEmailCommandHandler : ICommandHandler<ResendActivationEmailCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<ResendActivationEmailCommandHandler> _logger;
        // Placeholder: This should ideally come from configuration or a dedicated service
        private const string ActivationLinkBaseUrl = "https://localhost:7026/api/v1/users/activate";

        public ResendActivationEmailCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<ResendActivationEmailCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<Result> Handle(ResendActivationEmailCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to resend activation email for: {Email}", command.Email);

            var user = await _userRepository.GetByEmailAsync(command.Email);
            if(user is null)
            {
                _logger.LogWarning("Resend activation failed: User not found for email {Email}.", command.Email);
                // It's often better not to reveal if an email exists or not for this kind of endpoint
                // for security reasons (to prevent email enumeration).
                // So, we can return a generic success-like message or a specific "if your email is registered..."
                // For simplicity and consistency with other errors for now:
                return Result.Failure(new Error(
                    "User.NotFound",
                    "If an account with this email exists and requires activation, a new email has been sent.",
                    ErrorType.NotFound)); // Or ErrorType.Validation if considering the input potentially invalid
            }

            if (user.IsActive)
            {
                _logger.LogInformation("Account for email {Email} is already active. No activation email resent.", command.Email);
                return Result.Success(); // Account is already active, consider this a success.
            }

            if (user.IsDeleted)
            {
                _logger.LogWarning("Resend activation failed: Account for email {Email} is deleted.", command.Email);
                return Result.Failure(new Error(
                   "User.Deleted",
                   "This account has been deleted and cannot be activated.",
                   ErrorType.Conflict));
            }

            // Generate a new activation token (this also sets IsActive = false and updates expiry)
            try
            {
                user.GenerateActivationToken(); // Domain method to create a new token
                _logger.LogInformation("New activation token generated for user {UserId}, email {Email}.", user.Id, command.Email);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error generating new activation token for user {UserId}", user.Id);
                return Result.Failure(new Error("Token.GenerationError", "Could not generate a new activation token.", ErrorType.Failure));
            }

            // The User entity has its ActivationToken and ActivationTokenExpiryUtc updated.
            // We need to persist these changes.
            // No explicit call to _userRepository.UpdateAsync(user) is needed if using EF Core change tracking.
            // The UnitOfWork will save the changes made to the tracked 'user' entity.

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("New activation token for user {UserId} persisted.", user.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException while saving new activation token for User ID: {UserId}", user.Id);
                return Result.Failure(new Error("Token.PersistenceError.DbUpdate", "A database error occurred while saving the new activation token.", ErrorType.Failure));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic exception while saving new activation token for User ID: {UserId}", user.Id);
                return Result.Failure(new Error("Token.PersistenceError.Generic", "An unexpected error occurred while saving the new activation token.", ErrorType.Failure));
            }

            // Send the new activation email
            if (string.IsNullOrEmpty(user.ActivationToken))
            {
                // This should not happen if GenerateActivationToken worked correctly
                _logger.LogError("Activation token is unexpectedly null after generation for user {UserId}", user.Id);
                return Result.Failure(new Error("Token.MissingAfterGeneration", "Failed to prepare activation email: token missing.", ErrorType.Failure));
            }

            string activationLink = $"{ActivationLinkBaseUrl}?token={Uri.EscapeDataString(user.ActivationToken)}&email={Uri.EscapeDataString(user.Email)}";
            string emailSubject = "Activate Your UMS Account (New Link)";
            string emailHtmlBody = $"<h1>Activate Your UMS Account</h1><p>We received a request to resend your account activation link. Please activate your account by clicking the link below:</p><p><a href='{activationLink}'>Activate Account</a></p><p>If you did not request this, please ignore this email.</p><p>Token: {user.ActivationToken}</p>"; // Token in body for easy testing

            bool emailSent = await _emailService.SendEmailAsync(user.Email, emailSubject, emailHtmlBody);
            if (!emailSent)
            {
                _logger.LogWarning("Failed to resend activation email simulation to {Email} for user {UserId}.", user.Email, user.Id);
                // Even if email sending fails, the token was generated and saved.
                // You might return a success but log the email issue, or return a specific error.
                // For now, let's indicate an issue but consider the token part done.
                return Result.Failure(new Error("Email.SendFailed", "Account activation link has been updated, but there was an issue resending the email. Please try activating later or contact support.", ErrorType.Failure));
            }

            _logger.LogInformation("New activation email simulation sent to {Email} for user {UserId}.", user.Email, user.Id);
            return Result.Success();
        }
    }
}
