using Mediator;
using UMS.SharedKernal;

namespace UMS.Application.Common.Messaging.Commands
{
    /// <summary>
    /// Handles a command that returns a specific response type wrapped in a Result.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <typeparam name="TResponse">The type of the response returned on success.</typeparam>
    public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
        where TCommand : ICommand<TResponse>
    {
        // Inherits the Handle method signature:
        // Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Handles a command that returns a non-generic Result (indicating success/failure only).
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
        where TCommand : ICommand
    {
        // Inherits the Handle method signature:
        // Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
    }
}
