namespace UMS.SharedKernal
{
    /// <summary>
    /// Represents the outcome of an operation that does not return a value on success.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Gets a value indicating whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets a value indicating whether the operation failed.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Gets the error associated with a failed operation.
        /// Returns Error.None if the operation was successful.
        /// </summary>
        public Error Error { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> class.
        /// Protected constructor to enforce creation via factory methods.
        /// </summary>
        /// <param name="isSuccess">Indicates if the operation was successful.</param>
        /// <param name="error">The error if the operation failed. Should be Error.None for success.</param>
        protected Result(bool isSuccess, Error error)
        {
            // Basic validation: A successful result should not have a specific error.
            // A failed result must have an error.
            if (isSuccess && error != Error.None)
            {
                throw new InvalidOperationException("A successful result cannot have an error.");
            }
            if (!isSuccess && error == Error.None)
            {
                throw new InvalidOperationException("A failed result must have an error.");
            }

            IsSuccess = isSuccess;
            Error = error;
        }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <returns>A new instance of <see cref="Result"/> indicating success.</returns>
        public static Result Success() => new Result(true, Error.None);

        /// <summary>
        /// Creates a failed result with the specified error.
        /// </summary>
        /// <param name="error">The error that occurred.</param>
        /// <returns>A new instance of <see cref="Result"/> indicating failure.</returns>
        public static Result Failure(Error error) => new Result(false, error);

        /// <summary>
        /// Creates a failed result with the specified error for a generic Result type.
        /// Useful for easily converting non-generic failures to generic ones.
        /// </summary>
        /// <typeparam name="TValue">The type of the value for the generic result.</typeparam>
        /// <param name="error">The error that occurred.</param>
        /// <returns>A new instance of <see cref="Result{TValue}"/> indicating failure.</returns>
        public static Result<TValue> Failure<TValue>(Error error) => new Result<TValue>(default, false, error);
    }

    /// <summary>
    /// Represents the outcome of an operation that returns a value on success.
    /// </summary>
    /// <typeparam name="TValue">The type of the value returned on success.</typeparam>
    public class Result<TValue> : Result
    {
        private readonly TValue? _value;

        /// <summary>
        /// Gets the value of the successful operation.
        /// Accessing this property on a failed result will throw an exception.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if accessed when IsFailure is true.</exception>
        public TValue Value => IsSuccess
            ? _value! // Null-forgiving operator used here as IsSuccess ensures _value is not null for success.
            : throw new InvalidOperationException("Cannot access the value of a failed result. Check IsSuccess first.");

        /// <summary>
        /// Initializes a new instance of the <see cref="Result{TValue}"/> class.
        /// Protected constructor to enforce creation via factory methods.
        /// </summary>
        /// <param name="value">The value if the operation was successful.</param>
        /// <param name="isSuccess">Indicates if the operation was successful.</param>
        /// <param name="error">The error if the operation failed. Should be Error.None for success.</param>
        protected internal Result(TValue? value, bool isSuccess, Error error)
            : base(isSuccess, error)
        {
            _value = value;
        }

        /// <summary>
        /// Creates a successful result with the specified value.
        /// </summary>
        /// <param name="value">The value to be returned.</param>
        /// <returns>A new instance of <see cref="Result{TValue}"/> indicating success.</returns>
        public static Result<TValue> Success(TValue value) => new Result<TValue>(value, true, Error.None);

        // Implicit conversion from TValue to Result<TValue> for convenience
        public static implicit operator Result<TValue>(TValue value) => Success(value);
    }
}
