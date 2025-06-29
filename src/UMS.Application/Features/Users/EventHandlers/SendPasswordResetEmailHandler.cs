using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UMS.Application.Abstractions.Services;
using UMS.Application.Settings;
using UMS.Domain.Users.Events;

namespace UMS.Application.Features.Users.EventHandlers
{
    public class SendPasswordResetEmailHandler : INotificationHandler<UserPasswordResetRequestedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ClientAppSettings _clientAppSettings;
        private readonly ILogger<SendPasswordResetEmailHandler> _logger;

        public SendPasswordResetEmailHandler(IEmailService emailService, IOptions<ClientAppSettings> clientAppSettings, ILogger<SendPasswordResetEmailHandler> logger)
        {
            _emailService = emailService;
            _clientAppSettings = clientAppSettings.Value;
            _logger = logger;
        }

        public async Task Handle(UserPasswordResetRequestedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling UserPasswordResetRequestedEvent for user {UserId}...", notification.UserId);

            string resetLink = $"{_clientAppSettings.PasswordResetLinkBaseUrl}?token={Uri.EscapeDataString(notification.ResetToken)}&email={Uri.EscapeDataString(notification.Email)}";
            string subject = "Reset Your UMS Password";
            string body = $"<h1>Password Reset Request</h1><p>Please reset your password by clicking the link below:</p><p><a href='{resetLink}'>Reset Password</a></p>";

            bool emailSent = await _emailService.SendEmailAsync(notification.Email, subject, body);

            if (emailSent)
            {
                _logger.LogInformation("Password reset email sent successfully to {Email} for user {UserId}.", notification.Email, notification.UserId);
            }
            else
            {
                _logger.LogError("Failed to send password reset email to {Email} for user {UserId}.", notification.Email, notification.UserId);
            }
        }
    }
}
