using Mediator;
using UMS.SharedKernal;

namespace UMS.Application.Common.Messaging.Queries
{
    /// <summary>
    /// Handles a query and returns a specific response type wrapped in a Result.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query.</typeparam>
    /// <typeparam name="TResponse">The type of the data being queried.</typeparam>
    public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
        where TQuery : IQuery<TResponse>
    {
        // Inherits the Handle method signature:
        // Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
    }
}
