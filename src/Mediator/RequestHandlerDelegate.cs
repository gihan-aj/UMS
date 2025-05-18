using System.Threading.Tasks;

namespace Mediator
{
    /// <summary>
    /// Represents the next action in the pipeline.
    /// This could be the next behavior or the actual request handler.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a result of TResponse.</returns>
    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
}
