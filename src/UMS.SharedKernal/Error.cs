namespace UMS.SharedKernel
{
    /// <summary>
    /// Represents an error that occurred during an operation.
    /// </summary>
    public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
    {
        /// <summary>
        /// Represents no error. Used for successful results.
        /// </summary>
        public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

        /// <summary>
        /// Represents a generic failure error when no specific code is provided.
        /// </summary>
        public static readonly Error DefaultFailure = new("General.Failure", "An unexpected error occurred.", ErrorType.Failure);

        /// <summary>
        /// Represents a validation error.
        /// </summary>
        public static readonly Error ValidationError = new("General.Validation", "A validation error occurred.", ErrorType.Validation);

        /// <summary>
        /// Represents a "Not Found" error.
        /// </summary>
        public static readonly Error NotFound = new("General.NotFound", "The requested resource was not found.", ErrorType.NotFound);

        /// <summary>
        /// Represents an unauthorized access error.
        /// </summary>
        public static readonly Error Unauthorized = new("General.Unauthorized", "Unauthorized access.", ErrorType.Unauthorized);

        /// <summary>
        /// Represents a conflict error (e.g., resource already exists).
        /// </summary>
        public static readonly Error Conflict = new("General.Conflict", "A conflict occurred with the current state of the resource.", ErrorType.Conflict);
    }
}
