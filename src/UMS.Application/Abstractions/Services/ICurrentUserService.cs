using System;
using System.Collections.Generic;

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

        /// <summary>
        /// A set of the current user's role names for fast lookups.
        /// </summary>
        HashSet<string> RoleNames { get; }
    }
}
