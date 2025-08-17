using System;
using UMS.Domain.Primitives;

namespace UMS.Domain.Clients
{
    /// <summary>
    /// Represents a single redirect URI for a Client.
    /// </summary>
    public class ClientRedirectUri : Entity<int>
    {
        public string Uri { get; private set; } = string.Empty;
        public Guid ClientId { get; private set; } // Foreign key to Client

        private ClientRedirectUri() { }

        public static ClientRedirectUri Create(string uri, Guid clientId)
        {
            // Add validation for parameters
            if (string.IsNullOrWhiteSpace(uri))
                throw new ArgumentException("Uri cannot be empty.", nameof(uri));

            return new ClientRedirectUri
            {
                Uri = uri,
                ClientId = clientId,
            };
        }
    }
}
