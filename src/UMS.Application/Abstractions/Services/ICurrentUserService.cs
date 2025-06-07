using System;

namespace UMS.Application.Abstractions.Services
{
    /// <summary>
    /// Defines a service that provides information about the currently authenticated user.
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Gets the unique identifier of the currently authenticated user.
        /// Returns null if the user is not authenticated.
        /// </summary>
        Guid? UserId { get; }

        /// <summary>
        /// Gets the email of the currently authenticated user.
        /// Returns null if the user is not authenticated.
        /// </summary>
        string? UserEmail { get; }

        // Could add other properties like IsAuthenticated, roles, etc.
    }
}
