using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using UMS.Application.Features.Users.Notifications;

namespace UMS.Application.Features.Users.NotificationHandlers
{
    public class SendWelcomeEmailOnUserCreatedHandler : INotificationHandler<UserCreatedNotification>
    {
        private readonly ILogger<SendWelcomeEmailOnUserCreatedHandler> _logger;

        public SendWelcomeEmailOnUserCreatedHandler(ILogger<SendWelcomeEmailOnUserCreatedHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "----- [Notification Handler] Simulating sending welcome email to {Email} for User ID: {UserId} at {Timestamp} -----",
                notification.Email,
                notification.UserId,
                notification.Timestamp);

            // In a real application, you would integrate with an email service here.
            // For example: await _emailService.SendWelcomeEmailAsync(notification.Email, notification.UserId);

            return Task.CompletedTask;
        }
    }
}
