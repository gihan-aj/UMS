using Mediator;
using UMS.SharedKernel;

namespace UMS.Application.Common.Messaging.Queries
{
    /// <summary>
    /// Represents a query operation that retrieves data and returns a specific response type
    /// upon successful completion, wrapped in a Result. Queries should not modify state.
    /// </summary>
    /// <typeparam name="TResponse">The type of the data being queried.</typeparam>
    public interface IQuery<TResponse> : IRequest<Result<TResponse>>
    {
        // Marker interface inheriting from IRequest with a Result<TResponse>
    }
}
