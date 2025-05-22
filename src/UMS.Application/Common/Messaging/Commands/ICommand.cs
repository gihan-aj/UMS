using Mediator;
using UMS.SharedKernel;

namespace UMS.Application.Common.Messaging.Commands
{
    /// <summary>
    /// Represents a command operation that returns a specific response type upon successful completion,
    /// wrapped in a Result. Commands typically modify state.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response returned on success.</typeparam>
    public interface ICommand<TResponse> : IRequest<Result<TResponse>>
    {
        // Marker interface inheriting from IRequest with a Result<TResponse>
    }

    /// <summary>
    /// Represents a command operation that does not return a specific value upon successful completion,
    /// but still returns a Result to indicate success or failure. Commands typically modify state.
    /// </summary>
    public interface ICommand : IRequest<Result>
    {
        // Marker interface inheriting from IRequest with a non-generic Result
    }
}
