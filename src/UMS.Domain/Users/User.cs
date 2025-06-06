﻿using System;
using System.Security.Cryptography;
using UMS.Domain.Primitives;
using UMS.Domain.Users.Events;

namespace UMS.Domain.Users
{
    /// <summary>
    /// Represents a User in the system. This is an Aggregate Root.
    /// </summary>
    public class User : AggregateRoot<Guid>, ISoftDeletable
    {
        public string UserCode { get; private set; } = string.Empty;

        public string Email { get; private set; }

        public string? FirstName { get; private set; }

        public string? LastName { get; private set; }

        public string PasswordHash { get; private set; }

        public bool IsActive { get; private set; }

        public DateTime? LastLoginAtUtc { get; private set; }

        // --- Activation Token Properties ---
        public string? ActivationToken { get; private set; }

        public DateTime? ActivationTokenExpiryUtc { get; private set; }

        // --- ISoftDeletable Implementation ---
        public bool IsDeleted { get; private set; }

        public DateTime? DeletedAtUtc { get; private set; }

        public Guid? DeletedBy { get; private set; }

        // Private constructor for ORM/Persistence frameworks and User.Create
        private User(Guid id, string userCode, string email, string passwordHash, string? firstName, string? lastName)
            : base(id)
        {
            // Basic validation for required fields should be here or handled before calling.
            // For a true rich model, setters are private, and changes go through methods.
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email cannot be empty.", nameof(email));
            if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));
            if (string.IsNullOrWhiteSpace(userCode)) throw new ArgumentException("User code cannot be empty.", nameof(userCode));

            UserCode = userCode;
            Email = email.ToLowerInvariant();
            PasswordHash = passwordHash;
            FirstName = firstName;
            LastName = lastName;
            IsActive = false; // New users are inactive by default
            IsDeleted = false;
            SetCreationAudit(null); // Placeholder for current user ID
        }

        // Required for EF Core
        private User() : base(Guid.NewGuid()) // Generates a new Guid for EF Core materialization if Id is not set by DB
        {
            // Initialize required non-nullable properties to default values
            // This is important for EF Core when it creates instances via a private parameterless constructor
            Email = string.Empty;
            PasswordHash = string.Empty;
            UserCode = string.Empty;
        }

        /// <summary>
        /// Factory method to create a new user.
        /// </summary>
        public static User Create(
            string userCode,
            string email,
            string passwordHash,
            string? firstName,
            string? lastName,
            Guid? createdByUserId // Pass current user id for auditing
            )
        {
            var userId = Guid.NewGuid();
            var user = new User(userId, userCode, email, passwordHash, firstName, lastName);
            user.SetCreationAudit(createdByUserId);
            user.GenerateActivationToken(); // Generate token upon creation

            // Raise a domain event
            user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id, user.Email, user.UserCode, user.CreatedAtUtc, user.ActivationToken!));

            return user;
        }

        // --- Activation Methods ---
        public void GenerateActivationToken(int expiryHours = 24)
        {
            // Generate cryptographically strong, URL-safe token
            // Example: 32 bytes, base64 URL encoded
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            ActivationToken = Convert.ToBase64String(tokenBytes)
                .TrimEnd('=') // Padding safe
                .Replace('+', '-') // URL-safe
                .Replace('/','_'); // URL-safe
            ActivationTokenExpiryUtc = DateTime.UtcNow.AddHours(expiryHours);
            IsActive = false; // Ensure user is inactive when a new token is generated
        }

        public bool ValidateActivationToken(string token)
        {
            if(string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(ActivationToken))
            {
                return false;
            }
            if(ActivationTokenExpiryUtc.HasValue && ActivationTokenExpiryUtc.Value < DateTime.UtcNow)
            {
                return false; // Token expired
            }

            return ActivationToken == token;
        }

        /// <summary>
        /// Activates the user's account.
        /// </summary>
        public void Activate(Guid? modifiedByUserId)
        {
            if (IsActive) return;
            if (IsDeleted)
            {
                throw new InvalidOperationException("Cannot activate a deleted user.");
            }

            IsActive = true;
            ActivationToken = null; // Clear the token once used
            ActivationTokenExpiryUtc = null;
            SetModificationAudit(modifiedByUserId);
            RaiseDomainEvent(new UserAccountActivatedDomainEvent(Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Deactivates the user's account.
        /// </summary>
        public void Deactivate(Guid? modifiedByUserId)
        {
            if (!IsActive)
            {
                return; // Or throw new InvalidOperationException("User is already inactive.");
            }
            if (IsDeleted)
            {
                throw new InvalidOperationException("Cannot deactivate a deleted user.");
            }

            IsActive = false;
            SetModificationAudit(modifiedByUserId);
            RaiseDomainEvent(new UserAccountDeactivatedDomainEvent(Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Changes the user's password.
        /// </summary>
        public void ChangePassword(string newPasswordHash, Guid? modifiedByUserId)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
            {
                throw new ArgumentException("New password hash cannot be empty.", nameof(newPasswordHash));
            }
            if (IsDeleted)
            {
                throw new InvalidOperationException("Cannot change password for a deleted user.");
            }

            PasswordHash = newPasswordHash;
            SetModificationAudit(modifiedByUserId);
            RaiseDomainEvent(new UserPasswordChangedDomainEvent(Id, DateTime.UtcNow));
            // Potentially: MarkAllSessionsAsInvalid();
        }

        /// <summary>
        /// Updates the user's profile information.
        /// </summary>
        public void UpdateProfile(string? firstName, string? lastName, Guid? modifiedByUserId)
        {
            if (IsDeleted)
            {
                throw new InvalidOperationException("Cannot update profile for a deleted user.");
            }
            // Add any validation for first/last name if necessary
            FirstName = firstName;
            LastName = lastName;
            SetModificationAudit(modifiedByUserId);
            RaiseDomainEvent(new UserProfileUpdatedDomainEvent(Id, FirstName, LastName, DateTime.UtcNow));
        }

        public void RecordLogin(Guid? modifiedByUserId)
        {
            LastLoginAtUtc = DateTime.UtcNow;
            //SetModificationAudit(modifiedByUserId); // Arguably, login might not be a "modification" in the auditable sense.
                                                    // You might handle this separately or not audit it as a modification.
        }

        // --- ISoftDeletable Implementation ---
        public void MarkAsDeleted(Guid? deletedByUserId)
        {
            if (IsDeleted)
            {
                return; // Already deleted
            }
            IsDeleted = true;
            DeletedAtUtc = DateTime.UtcNow;
            DeletedBy = deletedByUserId;
            IsActive = false; // Typically, a deleted user is also inactive.
            SetModificationAudit(deletedByUserId); // Also update modification audit
            RaiseDomainEvent(new UserSoftDeletedDomainEvent(Id, DeletedAtUtc.Value));
        }
    }
}
