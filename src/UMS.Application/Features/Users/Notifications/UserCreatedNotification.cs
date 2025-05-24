using System;
using Mediator;

namespace UMS.Application.Features.Users.Notifications
{
    public class UserCreatedNotification : INotification
    {
        public Guid UserId { get; }

        public string Email { get; }

        public DateTime Timestamp { get; }

        public UserCreatedNotification(Guid userId, string email)
        {
            UserId = userId;
            Email = email;
            Timestamp = DateTime.UtcNow;
        }
    }
}
