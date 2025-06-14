using System;
using System.Security.Claims;
using UMS.Domain.Users;

namespace UMS.Application.Abstractions.Services
{
    /// <summary>
    /// Interface for a service that generates JWT authentication tokens.
    /// </summary>
    public interface IJwtTokenGeneratorService
    {
        /// <summary>
        /// Generates a JWT token for the specified user.
        /// </summary>
        /// <param name="user">The user for whom to generate the token.</param>
        /// <returns>The generated JWT token string and its expiry time.</returns>
        (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);

        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
