using System;
using System.Collections.Generic;
using System.Linq;
using UMS.Domain.Primitives;
using UMS.Domain.Users;

namespace UMS.Domain.Clients
{
    /// <summary>
    /// Represents a client application that is registered to use the UMS for authentication.
    /// This is an Aggregate Root.
    /// </summary>
    public class Client : AggregateRoot<Guid>, ISoftDeletable
    {
        /// <summary>
        /// A unique, public identifier for the client application (e.g., "pos-system").
        /// </summary>
        public string ClientId { get; private set; } = string.Empty;

        /// <summary>
        /// A human-readable name for the application.
        /// </summary>
        public string ClientName { get; private set; } = string.Empty;

        /// <summary>
        /// The hashed secret for confidential clients.
        /// </summary>
        public string ClientSecretHash { get; private set; } = string.Empty;

        /// <summary>
        /// A collection of allowed redirect URIs. After a user authenticates, the UMS will only
        /// redirect back to one of these URIs.
        /// </summary>
        public ICollection<ClientRedirectUri> RedirectUris { get; private set; } = new HashSet<ClientRedirectUri>();

        // ISoftDeletable Implementation
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAtUtc { get; private set; }
        public Guid? DeletedBy { get; private set; }

        // Private constructor for EF Core
        private Client() { }

        public static Client Create(string clientId, string clientName, string clientSecretHash, Guid? createdByUserId)
        {
            // Add validation for parameters
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("ClientId cannot be empty.", nameof(clientId));
            if (string.IsNullOrWhiteSpace(clientName))
                throw new ArgumentException("ClientName cannot be empty.", nameof(clientName));

            var client = new Client
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                ClientName = clientName,
                ClientSecretHash = clientSecretHash // Assume secret is already hashed
            };

            client.SetCreationAudit(createdByUserId);

            return client;
        }

        public void AddRedirectUris(List<string> uris)
        {
            foreach (var uri in uris)
            {
                if (!RedirectUris.Any(ru => ru.Uri.Equals(uri, StringComparison.OrdinalIgnoreCase)))
                {
                    RedirectUris.Add(ClientRedirectUri.Create(uri, Id));
                }
            }
        }

        public void Update(string newName, Guid? modifiedByUserId)
        {
            if (!string.IsNullOrWhiteSpace(newName))
            {
                ClientName = newName;
                SetModificationAudit(modifiedByUserId);
            }
        }

        public void MarkAsDeleted(Guid? deletedByUserId)
        {
            if (IsDeleted) return;
            IsDeleted = true;
            DeletedAtUtc = DateTime.UtcNow;
            DeletedBy = deletedByUserId;
            SetModificationAudit(deletedByUserId);
        }
    }
}
