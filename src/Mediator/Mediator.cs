
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator
{
    public class Mediator : ISender
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
    }

}
