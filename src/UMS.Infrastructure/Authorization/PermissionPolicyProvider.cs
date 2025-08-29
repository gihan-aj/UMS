using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace UMS.Infrastructure.Authorization
{
    /// <summary>
    /// A custom policy provider that creates authorization policies dynamically
    /// for any permission string starting with our defined prefix (e.g., "users:").
    /// </summary>
    public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
        }

        // This method is called by the authorization framework to get a policy for a given name
        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // First, check if a policy with this name is already configured.
            var policy =  await base.GetPolicyAsync(policyName);
            if(policy != null)
            {
                return policy;
            }

            // If not, and if the policy name looks like one of our permissions,
            // create a new policy on the fly that uses our PermissionRequirement.
            // This avoids having to register hundreds of policies manually.
            if (policyName.Contains(':')) // Simple check to identify our permission format
            {
                var policyBuilder = new AuthorizationPolicyBuilder();
                // ...require the user to be authenticated via the JWT Bearer scheme...
                policyBuilder.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                // ...and add our custom permission requirement.
                policyBuilder.AddRequirements(new PermissionRequirement(policyName));
                return policyBuilder.Build();
            }

            // No policy found
            return null;
        }
    }
}
