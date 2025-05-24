
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator
{
    public class Mediator : ISender, IPublisher
    {
        private readonly IServiceProvider _serviceProvider;

        public Mediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Sends a request through the pipeline to its handler and returns a response.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The request object.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the send operation. The task result contains the handler's response.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the request is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if no handler is found for the request type or if a behavior/handler's Handle method is missing.</exception>
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var requestType = request.GetType();
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
            var handlerInstance = _serviceProvider.GetService(handlerType);

            if (handlerInstance == null)
            {
                throw new InvalidOperationException($"No handler found for request type {requestType.Name} and response type {typeof(TResponse).Name}. Ensure it is registered with the DI container.");
            }

            RequestHandlerDelegate<TResponse> actualHandlerDelegate = () =>
            {
                var handleMethod = handlerInstance.GetType().GetMethod("Handle", new[] { requestType, typeof(CancellationToken) });
                if (handleMethod == null)
                {
                    throw new InvalidOperationException($"Could not find Handle method on handler {handlerInstance.GetType().Name} for request {requestType.Name}");
                }
                return (Task<TResponse>)handleMethod.Invoke(handlerInstance, new object[] { request, cancellationToken })!;
            };

            var pipelineBehaviorInterfaceType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
            var behaviors = _serviceProvider.GetServices(pipelineBehaviorInterfaceType)
                                           .Cast<object>()
                                           .Reverse()
                                           .ToList();

            RequestHandlerDelegate<TResponse> chainedDelegate = behaviors
                .Aggregate(actualHandlerDelegate, (nextOperationDelegate, behavior) =>
                {
                    var behaviorHandleMethod = behavior.GetType().GetMethod("Handle", new[] { requestType, typeof(CancellationToken), typeof(RequestHandlerDelegate<TResponse>) });
                    if (behaviorHandleMethod == null)
                    {
                        throw new InvalidOperationException($"Could not find Handle method on behavior {behavior.GetType().Name} for request {requestType.Name}");
                    }
                    return () => (Task<TResponse>)behaviorHandleMethod.Invoke(behavior, new object[] { request, cancellationToken, nextOperationDelegate })!;
                });

            return chainedDelegate();

        }

        /// <summary>
        /// Sends a request that does not return a value through the pipeline.
        /// </summary>
        /// <param name="request">The request object.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the send operation.</returns>
        public Task Send(IRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Send((IRequest<Unit>)request, cancellationToken);
        }

        // --- IPublisher Implementation ---

        /// <summary>
        /// Publishes the specified notification to all relevant handlers.
        /// This is the generic overload for convenience and type safety.
        /// </summary>
        /// <typeparam name="TNotification">The type of the notification.</typeparam>
        /// <param name="notification">The notification to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the asynchronous publish operation.
        /// This task completes when all handlers have processed the notification.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the notification is null.</exception>
        public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            // Get the actual type of the notification object (e.g., UserCreatedNotification)
            var notificationType = notification.GetType();

            // Construct the generic notification handler type (e.g., INotificationHandler<UserCreatedNotification>)
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);

            // Resolve all registered handlers for this specific notification type
            // GetServices returns IEnumerable<object>. Each object is an instance of the specific handler.
            var handlers = _serviceProvider.GetServices(handlerType);

            var tasks = new List<Task>();

            foreach (var handler in handlers)
            {
                if (handler == null) continue; // Should not happen if DI is configured correctly

                // The handler object is of type object, so we need to dynamically invoke the Handle method.
                var handleMethod = handler.GetType().GetMethod("Handle", new[] { notificationType, typeof(CancellationToken) });

                if (handleMethod == null)
                {
                    // This might indicate an issue with the handler implementation or our reflection logic
                    // For robustness, you might log this or handle it differently.
                    // For now, we'll skip if the method isn't found, though ideally, it should always be there for a valid handler.
                    // Or, throw an exception:
                    // throw new InvalidOperationException($"Could not find Handle method on handler {handler.GetType().Name} for notification {notificationType.Name}");
                    continue;
                }

                // Invoke the Handle method. It returns a Task.
                // We add this task to a list to await them all.
                // This ensures all handlers are invoked. Depending on the strategy (e.g., parallel, sequential, wait for all),
                // this part might change. Task.WhenAll is a common approach.
                tasks.Add((Task)handleMethod.Invoke(handler, new object[] { notification, cancellationToken })!);
            }

            // Wait for all handler tasks to complete.
            // If any handler throws an exception, Task.WhenAll will propagate the first one.
            // Consider error handling strategies here (e.g., AggregateException).
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Publishes the specified notification to all relevant handlers.
        /// This non-generic version is useful when the notification type isn't known at compile time.
        /// </summary>
        /// <param name="notification">The notification to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the asynchronous publish operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the notification is null.</exception>
        public Task Publish(INotification notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }
            // This is a bit tricky because Publish<TNotification> is generic.
            // We need to call the generic method using reflection, or have a common non-generic path.
            // One way is to get the generic method and make it concrete with the notification's actual type.

            var notificationType = notification.GetType();
            var publishMethod = GetType().GetMethod(nameof(Publish), 1, new[] { notificationType, typeof(CancellationToken) });

            if (publishMethod == null) // Should find the generic Publish<TNotification>
            {
                // Fallback or error if specific generic method isn't found,
                // this might happen if the method signature changes or isn't public.
                // This GetMethod call looks for a method named "Publish" that has 1 generic argument
                // and takes parameters of types (notificationType, CancellationToken).
                // The `1` indicates the number of generic type parameters for the method itself.
                throw new InvalidOperationException($"Could not find the generic Publish<{notificationType.Name}>(...) method.");
            }

            // Make the generic method concrete with the actual notification type
            var concretePublishMethod = publishMethod.MakeGenericMethod(notificationType);

            // Invoke it. The result is a Task.
            return (Task)concretePublishMethod.Invoke(this, new object[] { notification, cancellationToken })!;
        }
    }

}
