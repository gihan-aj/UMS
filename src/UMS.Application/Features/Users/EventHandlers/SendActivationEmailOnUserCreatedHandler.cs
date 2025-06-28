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
    /// <summary>
    /// Handles the UserCreatedDomainEvent to send an activation email.
    /// </summary>
    public class SendActivationEmailOnUserCreatedHandler : INotificationHandler<UserCreatedDomainEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ClientAppSettings _clientAppSettings;
        private readonly ILogger<SendActivationEmailOnUserCreatedHandler> _logger;

        public SendActivationEmailOnUserCreatedHandler(IEmailService emailService, ILogger<SendActivationEmailOnUserCreatedHandler> logger, IOptions<ClientAppSettings> clientAppSettings)
        {
            _emailService = emailService;
            _logger = logger;
            _clientAppSettings = clientAppSettings.Value;
        }

        public async Task Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling UserCreatedDomainEvent for user {UserId}...", notification.UserId);

            // Construct the activation link using the token from the event
            string activationLink = $"{_clientAppSettings.ActivationLinkBaseUrl}?token={Uri.EscapeDataString(notification.ActivationToken)}&email={Uri.EscapeDataString(notification.Email)}";
            string emailSubject = "Activate Your UMS Account";
            string emailHtmlBody = $"<h1>Welcome to UMS!</h1><p>Please activate your account by clicking the link below:</p><p><a href='{activationLink}'>Activate Account</a></p>";

            bool emailSent = await _emailService.SendEmailAsync(notification.Email, emailSubject, emailHtmlBody);

            if (emailSent)
            {
                _logger.LogInformation("Activation email sent successfully to {Email} for user {UserId}.", notification.Email, notification.UserId);
            }
            else
            {
                _logger.LogError("Failed to send activation email to {Email} for user {UserId}.", notification.Email, notification.UserId);
            }
        }
    }
}
