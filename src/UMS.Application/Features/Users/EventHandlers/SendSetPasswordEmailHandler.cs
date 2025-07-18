using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Services;
using UMS.Application.Settings;
using UMS.Domain.Users.Events;

namespace UMS.Application.Features.Users.EventHandlers
{
    /// <summary>
    /// Handles the AdminCreatedUserEvent to send an email prompting the user to set their initial password.
    /// </summary>
    public class SendSetPasswordEmailHandler : INotificationHandler<AdminCreatedUserDomainEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ClientAppSettings _clientAppSettings;
        private readonly ILogger<SendSetPasswordEmailHandler> _logger;

        public SendSetPasswordEmailHandler(
            IEmailService emailService,
            IOptions<ClientAppSettings> clientAppSettings,
            ILogger<SendSetPasswordEmailHandler> logger)
        {
            _emailService = emailService;
            _clientAppSettings = clientAppSettings.Value;
            _logger = logger;
        }

        public async Task Handle(AdminCreatedUserDomainEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling AdminCreatedUserEvent for user {UserId}...", notification.UserId);

            // This link should point to the frontend page for setting an initial password
            string setPasswordLink = $"{_clientAppSettings.ActivationLinkBaseUrl}?token={Uri.EscapeDataString(notification.ActivationToken)}&email={Uri.EscapeDataString(notification.Email)}&setPassword=true";
            string subject = "Your UMS Account has been created";
            string body = $"<h1>Welcome to UMS!</h1><p>An administrator has created an account for you. Please set your password and activate your account by clicking the link below:</p><p><a href='{setPasswordLink}'>Set Password & Activate</a></p>";

            await _emailService.SendEmailAsync(notification.Email, subject, body);
        }
    }
}
