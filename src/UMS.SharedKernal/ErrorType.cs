namespace UMS.SharedKernal
{
    /// <summary>
    /// Defines types of errors for categorization.
    /// </summary>
    public enum ErrorType
    {
        /// <summary>No error.</summary>
        None = 0,
        /// <summary>A general failure.</summary>
        Failure = 1,
        /// <summary>A validation error (e.g., invalid input).</summary>
        Validation = 2,
        /// <summary>A resource was not found.</summary>
        NotFound = 3,
        /// <summary>Unauthorized access.</summary>
        Unauthorized = 4,
        /// <summary>A conflict with the current state of a resource.</summary>
        Conflict = 5,
        // Add other specific error types as needed
    }
}
