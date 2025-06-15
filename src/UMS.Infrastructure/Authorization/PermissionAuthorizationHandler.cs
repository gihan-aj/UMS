using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace UMS.Infrastructure.Authorization
{
    /// <summary>
    /// A custom authorization handler that checks if a user has a specific permission claim.
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ILogger<PermissionAuthorizationHandler> _logger;

        public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            PermissionRequirement requirement)
        {
            // The "permission" claim type is what we added in our JwtTokenGeneratorService.
            var permissions = context.User.FindAll("permission");
            if (permissions.Any(p => p.Value == requirement.Permission))
            {
                // User has the requirement. So, the requirement is met
                _logger.LogInformation("Authorization Succeeded. User has required permission: {Permission}", requirement.Permission);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Authorization Failed. User does not have required permission: {Permission}", requirement.Permission);
                // The requirement is not met. We don't call context.Fail() here because
                // other handlers might still succeed. The requirement is simply not met by this handler.
            }

            return Task.CompletedTask;
        }
    }
}
