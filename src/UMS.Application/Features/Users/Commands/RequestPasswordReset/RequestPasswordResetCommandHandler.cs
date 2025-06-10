using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.RequestPasswordReset
{
    public class RequestPasswordResetCommandHandler : ICommandHandler<RequestPasswordResetCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<RequestPasswordResetCommandHandler> _logger;
        // Placeholder: Base URL for the password reset page on your frontend application
        private const string PasswordResetLinkBaseUrl = "https://your-frontend-app.com/reset-password";

        public RequestPasswordResetCommandHandler(
            IUserRepository userRepository, 
            IUnitOfWork unitOfWork, 
            IEmailService emailService, 
            ILogger<RequestPasswordResetCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<Result> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing password reset request for email: {Email}", command.Email);

            var user = await _userRepository.GetByEmailAsync(command.Email);

            // For security, do not reveal if an email address is registered or not.
            // Always return a success-like response, but only perform actions if the user exists and is valid.
            if (user is null || !user.IsActive || user.IsDeleted )
            {
                _logger.LogWarning("Password reset request for non-existent, deleted, or inactive email: {Email}. Responding with generic success to prevent enumeration.", command.Email);
                return Result.Success(); // Do nothing, but don't tell the client.
            }

            try
            {
                user.GeneratePasswordResetToken(); // Domain method generates new token and expiry
                _logger.LogInformation("Generated password reset token for user {UserId}", user.Id);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Saved password reset token for user {UserId}", user.Id);

                // Send password reset email
                string resetLink = $"{PasswordResetLinkBaseUrl}?token={Uri.EscapeDataString(user.PasswordResetToken!)}&email={Uri.EscapeDataString(user.Email)}";
                string subject = "Reset Your UMS Password";
                string body = $"<h1>Password Reset Request</h1><p>Please reset your password by clicking the link below:</p><p><a href='{resetLink}'>Reset Password</a></p><p>This link will expire in 30 minutes.</p><p>If you did not request this, please ignore this email.</p>";

                await _emailService.SendEmailAsync(user.Email, subject, body);
                _logger.LogInformation("Password reset email simulation sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during password reset request for user {UserId}", user.Id);
                // Even on failure, return a generic success to prevent leaking information.
            }

            return Result.Success();
        }
    }
}
