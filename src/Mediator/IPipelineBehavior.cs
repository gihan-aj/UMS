using System.Threading.Tasks;
using System.Threading;

namespace Mediator
{
    /// <summary>
    /// Defines a handler for a request/response pipeline.
    /// Implementations can be used to add cross-cutting concerns like logging, validation, caching, etc.
    /// Behaviors are executed in the order they are registered with the DI container.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request. Must implement IRequest&lt;TResponse&gt;.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        /// <summary>
        /// Handles the request as part of a pipeline.
        /// </summary>
        /// <param name="request">The request instance.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="next">The delegate to call to continue to the next step in the pipeline.
        /// This will eventually call the next behavior or the actual request handler.</param>
        /// <returns>A task representing the asynchronous operation, with a result of TResponse.</returns>
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next);
    }
}
