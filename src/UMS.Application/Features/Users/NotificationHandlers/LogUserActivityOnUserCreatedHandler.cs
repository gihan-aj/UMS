using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using UMS.Application.Features.Users.Notifications;

namespace UMS.Application.Features.Users.NotificationHandlers
{
    public class LogUserActivityOnUserCreatedHandler : INotificationHandler<UserCreatedNotification>
    {
        private readonly ILogger<LogUserActivityOnUserCreatedHandler> _logger;

        public LogUserActivityOnUserCreatedHandler(ILogger<LogUserActivityOnUserCreatedHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "----- [Notification Handler] User activity logged: New user created - ID: {UserId}, Email: {Email}, Timestamp: {Timestamp} -----",
                notification.UserId,
                notification.Email,
                notification.Timestamp);

            // In a real application, you might write to an audit trail, update a read model, etc.

            return Task.CompletedTask;
        }
    }
}
