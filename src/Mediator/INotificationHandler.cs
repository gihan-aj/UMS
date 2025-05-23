using System.Threading.Tasks;
using System.Threading;

namespace Mediator
{
    /// <summary>
    /// Defines a handler for a notification.
    /// Multiple handlers can exist for the same notification type.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification being handled. Must implement INotification.</typeparam>
    public interface INotificationHandler<in TNotification> where TNotification : INotification
    {
        /// <summary>
        /// Handles the specified notification.
        /// </summary>
        /// <param name="notification">The notification message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the asynchronous handling operation.</returns>
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }
}
