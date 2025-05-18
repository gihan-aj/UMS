namespace Mediator
{
    /// <summary>
    /// Represents a request that returns a response.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public interface IRequest<out TResponse>
    {
        // This interface is a marker and doesn't need members for now.
        // 'out TResponse' is used to make it covariant, allowing to use a more derived response type.
    }

    /// <summary>
    /// Represents a request that does not return a value (similar to a command).
    /// For consistency, we can make it inherit from IRequest<Unit> where Unit is a type representing void.
    /// </summary>
    public interface IRequest : IRequest<Unit>
    {

    }
}
