using System.Collections.Generic;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace UMS.Infrastructure.Authentication
{
    public static class IdentityServerConfig
    {
        // Defines the APIs that IdentityServer will protect.
        public static IEnumerable<ApiResource> GetApiResources() =>
            new List<ApiResource>
            {
                // Example: A resource for our UMS API itself
                new ApiResource("ums_api", "UMS API")
                {
                    Scopes = { "ums_api.full_access" }
                }
            };

        // Defines the scopes that clients can request.
        public static IEnumerable<ApiScope> GetApiScopes() =>
            new List<ApiScope>
            {
                new ApiScope("ums_api.full_access", "Full access to the UMS API")
            };

        // Defines the client applications that are allowed to use our UMS for authentication.
        public static IEnumerable<Client> GetClients() =>
            new List<Client>
            {
                // Example: A machine-to-machine client for service-to-service communication
                new Client
                {
                    ClientId = "service.client",
                    ClientSecrets = { new Secret("a_very_secure_secret_for_the_service_client".Sha256()) },
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes = { "ums_api.full_access" }
                },

                // Example: An interactive client for your Angular frontend
                new Client
                {
                    ClientId = "angular_client",
                    ClientName = "Angular Frontend",
                    AllowedGrantTypes = GrantTypes.Code,// The most secure interactive flow (Authorization Code Flow with PKCE)
                    RequirePkce = true,
                    RequireClientSecret = false, // Public clients like SPAs don't use a secret

                    RedirectUris = { "http://localhost:4200/auth-callback" }, // Where to redirect after login
                    PostLogoutRedirectUris = { "http://localhost:4200" }, // Where to redirect after logout
                    AllowedCorsOrigins = { "http://localhost:4200" },

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "ums_api.full_access"
                    },

                    AllowAccessTokensViaBrowser = true, // Required for SPAs
                    AccessTokenLifetime = 3600 // 1 hour
                }
            };
    }
}
