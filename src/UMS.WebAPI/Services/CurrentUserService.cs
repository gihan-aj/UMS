using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using UMS.Application.Abstractions.Services;

namespace UMS.WebAPI.Services
{
    /// <summary>
    /// Implements ICurrentUserService by accessing the HttpContext.
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gets the User ID from the 'sub' (Subject) or 'uid' claim of the authenticated user's JWT.
        /// </summary>
        public Guid? UserId
        {
            get
            {
                // We used 'sub' and a custom 'uid' claim in our JwtTokenGeneratorService.
                // JwtRegisteredClaimNames.Sub is the standard.
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    _httpContextAccessor.HttpContext?.User?.FindFirstValue("uid");

                if(Guid.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }
                return null; // User is not authenticated or no valid ID found
            }
        }

        /// <summary>
        /// Gets the User Email from the 'email' claim of the authenticated user's JWT.
        /// </summary>
        public string? UserEmail => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
    }
}
