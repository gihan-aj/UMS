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
    public class SendActivationEmailOnTokenRegeneratedHandler : INotificationHandler<UserActivationTokenRegeneratedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ClientAppSettings _clientAppSettings;
        private readonly ILogger<SendActivationEmailOnTokenRegeneratedHandler> _logger;

        public SendActivationEmailOnTokenRegeneratedHandler(IEmailService emailService, IOptions<ClientAppSettings> clientAppSettings, ILogger<SendActivationEmailOnTokenRegeneratedHandler> logger)
        {
            _emailService = emailService;
            _clientAppSettings = clientAppSettings.Value;
            _logger = logger;
        }

        public async Task Handle(UserActivationTokenRegeneratedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling UserActivationTokenRegeneratedEvent for user {UserId}...", notification.UserId);

            string activationLink = $"{_clientAppSettings.ActivationLinkBaseUrl}?token={Uri.EscapeDataString(notification.ActivationToken)}&email={Uri.EscapeDataString(notification.Email)}";
            string subject = "Activate Your UMS Account (New Link)";
            string body = $"<h1>Activate Your UMS Account</h1><p>We received a request to resend your account activation link...</p><p><a href='{activationLink}'>Activate Account</a></p>";

            bool emailSent = await _emailService.SendEmailAsync(notification.Email, subject, body);

            if (emailSent)
            {
                _logger.LogInformation("Activation email resent successfully to {Email} for user {UserId}.", notification.Email, notification.UserId);
            }
            else
            {
                _logger.LogError("Failed to resend activation email to {Email} for user {UserId}.", notification.Email, notification.UserId);
            }
        }
    }
}
