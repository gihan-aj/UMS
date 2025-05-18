using System.Threading;
using System.Threading.Tasks;

namespace Mediator
{
    /// <summary>
    /// Defines a mechanism for sending requests.
    /// </summary>
    public interface ISender
    {
        /// <summary>
        /// Sends a request and returns a response.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The request object.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the send operation. The task result contains the handler's response.</returns>
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a request that does not return a value.
        /// </summary>
        /// <param name="request">The request object.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the send operation.</returns>
        Task Send(IRequest request, CancellationToken cancellationToken = default);
    }
}
