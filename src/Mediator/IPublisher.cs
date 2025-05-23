using System.Threading.Tasks;
using System.Threading;

namespace Mediator
{
    /// <summary>
    /// Defines a mechanism for publishing notifications.
    /// </summary>
    public interface IPublisher
    {
        /// <summary>
        /// Publishes the specified notification to all relevant handlers.
        /// </summary>
        /// <param name="notification">The notification to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the asynchronous publish operation.</returns>
        Task Publish(INotification notification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes the specified notification to all relevant handlers.
        /// This is a generic overload for convenience and type safety when the notification type is known at compile time.
        /// </summary>
        /// <typeparam name="TNotification">The type of the notification.</typeparam>
        /// <param name="notification">The notification to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the asynchronous publish operation.</returns>
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
    }
}
