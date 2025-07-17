using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UMS.Domain.Primitives;
using UMS.Domain.Users.Events;

namespace UMS.Domain.Users
{
    /// <summary>
    /// Represents a user in the system. This class is the aggregate root for all user-related operations.
    /// </summary>
    /// <remarks>
    /// The User entity encapsulates all properties and behaviors of a user, such as authentication,
    /// profile management, role assignments, and state transitions (activation, deactivation, deletion).
    /// State changes are managed through domain methods, and side effects are communicated via domain events.
    /// </remarks>
    public class User : AggregateRoot<Guid>, ISoftDeletable
    {
        #region Fields

        private readonly List<RefreshToken> _refreshTokens = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the unique business identifier for the user.
        /// </summary>
        public string UserCode { get; private set; }

        /// <summary>
        /// Gets the user's unique email address.
        /// </summary>
        public string Email { get; private set; }

        /// <summary>
        /// Gets the user's first name.
        /// </summary>
        public string? FirstName { get; private set; }

        /// <summary>
        /// Gets the user's last name.
        /// </summary>
        public string? LastName { get; private set; }

        /// <summary>
        /// Gets the hashed password for the user.
        /// </summary>
        public string? PasswordHash { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the user's account is active and can sign in.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets the timestamp of the user's last login.
        /// </summary>
        public DateTime? LastLoginAtUtc { get; private set; }

        #endregion

        #region Token Properties

        /// <summary>
        /// Gets the token used for account activation.
        /// </summary>
        public string? ActivationToken { get; private set; }

        /// <summary>
        /// Gets the expiry date for the account activation token.
        /// </summary>
        public DateTime? ActivationTokenExpiryUtc { get; private set; }

        /// <summary>
        /// Gets the token used for resetting the password.
        /// </summary>
        public string? PasswordResetToken { get; private set; }

        /// <summary>
        /// Gets the expiry date for the password reset token.
        /// </summary>
        public DateTime? PasswordResetTokenExpiryUtc { get; private set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets the collection of roles assigned to the user.
        /// </summary>
        public ICollection<UserRole> UserRoles { get; private set; } = new HashSet<UserRole>();

        /// <summary>
        /// Gets a read-only collection of the user's refresh tokens.
        /// </summary>
        /// <remarks>
        /// This collection is exposed as read-only to prevent direct manipulation from outside the aggregate.
        /// Use the <see cref="AddRefreshToken"/> method to add new tokens.
        /// </remarks>
        public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

        #endregion

        #region ISoftDeletable Implementation

        /// <summary>
        /// Gets a value indicating whether the user has been soft-deleted.
        /// </summary>
        public bool IsDeleted { get; private set; }

        /// <summary>
        /// Gets the timestamp when the user was soft-deleted.
        /// </summary>
        public DateTime? DeletedAtUtc { get; private set; }

        /// <summary>
        /// Gets the ID of the user who performed the soft-delete.
        /// </summary>
        public Guid? DeletedBy { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Private constructor for ORM/Persistence frameworks and the factory method.
        /// </summary>
        private User(Guid id, string userCode, string email, string? passwordHash, string? firstName, string? lastName)
            : base(id)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email cannot be empty.", nameof(email));
            if (string.IsNullOrWhiteSpace(userCode)) throw new ArgumentException("User code cannot be empty.", nameof(userCode));

            UserCode = userCode;
            Email = email.ToLowerInvariant();
            PasswordHash = passwordHash;
            FirstName = firstName;
            LastName = lastName;
            IsActive = false; // New users are inactive by default until activated.
            IsDeleted = false;
        }

        /// <summary>
        /// Private parameterless constructor required by EF Core for materialization.
        /// </summary>
        private User() : base(Guid.NewGuid())
        {
            // EF Core requires a parameterless constructor. Initialize non-nullable reference types.
            Email = string.Empty;
            UserCode = string.Empty;
        }

        #endregion

        #region Factory Method

        /// <summary>
        /// Factory method to create and initialize a new <see cref="User"/> instance.
        /// </summary>
        /// <param name="userCode">The business identifier for the user.</param>
        /// <param name="email">The user's email address.</param>
        /// <param name="passwordHash">The initial hashed password (can be null if set later).</param>
        /// <param name="firstName">The user's first name.</param>
        /// <param name="lastName">The user's last name.</param>
        /// <param name="activationTokenExpiryHours">The number of hours the activation token is valid for.</param>
        /// <param name="createdByUserId">The ID of the user creating this user, for auditing.</param>
        /// <returns>A new <see cref="User"/> instance, initialized but not yet active.</returns>
        /// <remarks>
        /// This method generates an initial activation token and raises a <see cref="UserCreatedDomainEvent"/>.
        /// </remarks>
        public static User Create(
            string userCode,
            string email,
            string? passwordHash,
            string? firstName,
            string? lastName,
            int activationTokenExpiryHours,
            Guid? createdByUserId)
        {
            var user = new User(Guid.NewGuid(), userCode, email, passwordHash, firstName, lastName);
            user.SetCreationAudit(createdByUserId);
            user.GenerateActivationToken(activationTokenExpiryHours);

            user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id, user.Email, user.UserCode, user.CreatedAtUtc, user.ActivationToken!));

            return user;
        }

        #endregion

        #region Domain Methods

        /// <summary>
        /// Activates the user's account, allowing them to sign in.
        /// </summary>
        /// <param name="modifiedByUserId">The ID of the user performing the activation, for auditing.</param>
        /// <exception cref="InvalidOperationException">Thrown if the user is soft-deleted.</exception>
        /// <remarks>
        /// This action is idempotent. It also invalidates any existing activation tokens.
        /// Raises a <see cref="UserAccountActivatedDomainEvent"/> upon successful activation.
        /// </remarks>
        public void Activate(Guid? modifiedByUserId)
        {
            if (IsActive) return;
            if (IsDeleted) throw new InvalidOperationException("Cannot activate a deleted user.");

            IsActive = true;
            ActivationToken = null; // Invalidate token after use
            ActivationTokenExpiryUtc = null;
            SetModificationAudit(modifiedByUserId);

            RaiseDomainEvent(new UserAccountActivatedDomainEvent(Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Deactivates the user's account, preventing them from signing in.
        /// </summary>
        /// <param name="modifiedByUserId">The ID of the user performing the deactivation, for auditing.</param>
        /// <exception cref="InvalidOperationException">Thrown if the user is soft-deleted.</exception>
        /// <remarks>
        /// This action is idempotent. It clears any pending activation tokens to prevent re-activation.
        /// Raises a <see cref="UserAccountDeactivatedDomainEvent"/> upon successful deactivation.
        /// </remarks>
        public void Deactivate(Guid? modifiedByUserId)
        {
            if (!IsActive) return;
            if (IsDeleted) throw new InvalidOperationException("Cannot deactivate a deleted user.");

            IsActive = false;
            ActivationToken = null; // Prevent re-activation with an old token
            ActivationTokenExpiryUtc = null;
            SetModificationAudit(modifiedByUserId);

            RaiseDomainEvent(new UserAccountDeactivatedDomainEvent(Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Generates a new account activation token.
        /// </summary>
        /// <param name="expiryHours">The number of hours the token should be valid for. Defaults to 24.</param>
        /// <remarks>
        /// This method will set the user to inactive, ensuring they must use the new token to activate.
        /// It is used during user creation and can be used to resend an activation link.
        /// </remarks>
        public void GenerateActivationToken(int expiryHours = 24)
        {
            ActivationToken = GenerateUrlSafeToken();
            ActivationTokenExpiryUtc = DateTime.UtcNow.AddHours(expiryHours);
            IsActive = false; // Ensure user is inactive when a new token is generated
        }

        /// <summary>
        /// Regenerates the activation token for an existing user.
        /// </summary>
        /// <param name="expiryHours">The number of hours the new token should be valid for. Defaults to 24.</param>
        /// <exception cref="Exception">Thrown if token generation fails internally.</exception>
        /// <remarks>
        /// Raises a <see cref="UserActivationTokenRegeneratedEvent"/>.
        /// </remarks>
        public void RegenerateActivationToken(int expiryHours = 24)
        {
            GenerateActivationToken(expiryHours);

            if (ActivationToken is null)
            {
                throw new Exception("Failed to create an activation token.");
            }

            RaiseDomainEvent(new UserActivationTokenRegeneratedEvent(this.Id, this.Email, this.ActivationToken));
        }

        /// <summary>
        /// Validates a provided account activation token.
        /// </summary>
        /// <param name="token">The activation token to validate.</param>
        /// <returns><c>true</c> if the token is valid and not expired; otherwise, <c>false</c>.</returns>
        public bool ValidateActivationToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(ActivationToken))
            {
                return false;
            }

            if (ActivationTokenExpiryUtc.HasValue && ActivationTokenExpiryUtc.Value < DateTime.UtcNow)
            {
                return false; // Token expired
            }

            return ActivationToken == token;
        }

        /// <summary>
        /// Updates the user's profile information.
        /// </summary>
        /// <param name="firstName">The user's new first name.</param>
        /// <param name="lastName">The user's new last name.</param>
        /// <param name="modifiedByUserId">The ID of the user performing the update, for auditing.</param>
        /// <exception cref="InvalidOperationException">Thrown if the user is soft-deleted.</exception>
        /// <remarks>
        /// Raises a <see cref="UserProfileUpdatedDomainEvent"/>.
        /// </remarks>
        public void UpdateProfile(string? firstName, string? lastName, Guid? modifiedByUserId)
        {
            if (IsDeleted) throw new InvalidOperationException("Cannot update profile for a deleted user.");

            FirstName = firstName;
            LastName = lastName;
            SetModificationAudit(modifiedByUserId);

            RaiseDomainEvent(new UserProfileUpdatedDomainEvent(Id, FirstName, LastName, DateTime.UtcNow));
        }

        /// <summary>
        /// Sets or changes the user's password.
        /// </summary>
        /// <param name="newPasswordHash">The new hashed password.</param>
        /// <param name="modifiedByUserId">The ID of the user performing the change, for auditing.</param>
        /// <exception cref="ArgumentException">Thrown if the new password hash is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the user is soft-deleted.</exception>
        /// <remarks>
        /// Raises a <see cref="UserPasswordChangedDomainEvent"/>.
        /// Consider invalidating all active sessions (e.g., refresh tokens) after this action.
        /// </remarks>
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
        }

        /// <summary>
        /// Generates a password reset token and sets its expiry.
        /// </summary>
        /// <param name="expiryMinutes">The number of minutes the token should be valid for. Defaults to 30.</param>
        /// <remarks>
        /// Raises a <see cref="UserPasswordResetRequestedEvent"/> containing the token,
        /// intended for an email service to consume.
        /// </remarks>
        public void GeneratePasswordResetToken(int expiryMinutes = 30)
        {
            PasswordResetToken = GenerateUrlSafeToken();
            PasswordResetTokenExpiryUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

            RaiseDomainEvent(new UserPasswordResetRequestedEvent(this.Id, this.Email, this.PasswordResetToken));
        }

        /// <summary>
        /// Resets the user's password using a valid reset token.
        /// </summary>
        /// <param name="newPasswordHash">The new hashed password.</param>
        /// <param name="providedToken">The password reset token provided by the user.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the user is soft-deleted, the token is invalid, or the token has expired.
        /// </exception>
        /// <remarks>
        /// Upon successful reset, the password reset token is invalidated to prevent reuse.
        /// Raises a <see cref="UserPasswordChangedDomainEvent"/>.
        /// </remarks>
        public void ResetPassword(string newPasswordHash, string providedToken)
        {
            if (IsDeleted) throw new InvalidOperationException("Cannot reset password for a deleted user.");
            if (string.IsNullOrWhiteSpace(providedToken) || PasswordResetToken != providedToken)
            {
                throw new InvalidOperationException("Invalid password reset token.");
            }
            if (PasswordResetTokenExpiryUtc.HasValue && PasswordResetTokenExpiryUtc.Value < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Password reset token has expired.");
            }

            PasswordHash = newPasswordHash;
            PasswordResetToken = null; // Invalidate the token after use.
            PasswordResetTokenExpiryUtc = null;
            SetModificationAudit(this.Id); // The user is the actor here

            RaiseDomainEvent(new UserPasswordChangedDomainEvent(Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Assigns a role to the user.
        /// </summary>
        /// <param name="roleId">The ID of the role to assign.</param>
        /// <param name="assigningUserId">The ID of the user assigning the role, for auditing.</param>
        /// <remarks>
        /// This action is idempotent; it will not add a duplicate role.
        /// Raises a <see cref="UserRoleAssignedDomainEvent"/>.
        /// </remarks>
        public void AssignRole(byte roleId, Guid assigningUserId)
        {
            if (!UserRoles.Any(ur => ur.RoleId == roleId))
            {
                UserRoles.Add(new UserRole { RoleId = roleId, UserId = this.Id });
                SetModificationAudit(assigningUserId);

                RaiseDomainEvent(new UserRoleAssignedDomainEvent(this.Id, roleId));
            }
        }

        /// <summary>
        /// Adds a new refresh token to the user for a specific device.
        /// </summary>
        /// <param name="deviceId">The unique identifier for the device.</param>
        /// <param name="validity">The duration for which the refresh token is valid.</param>
        /// <returns>The newly created <see cref="RefreshToken"/>.</returns>
        public RefreshToken AddRefreshToken(string deviceId, TimeSpan validity)
        {
            var newRefreshToken = RefreshToken.Create(this, deviceId, validity);
            _refreshTokens.Add(newRefreshToken);
            return newRefreshToken;
        }

        /// <summary>
        /// Records the timestamp of a user login.
        /// </summary>
        /// <remarks>
        /// This method updates the <see cref="LastLoginAtUtc"/> timestamp.
        /// This is typically not considered an auditable modification of the entity's core data,
        /// so <see cref="AggregateRoot{Guid}.SetModificationAudit"/> is not called.
        /// </remarks>
        public void RecordLogin()
        {
            LastLoginAtUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the user entity as deleted (soft-delete).
        /// </summary>
        /// <param name="deletedByUserId">The ID of the user performing the deletion, for auditing.</param>
        /// <remarks>
        /// This action is idempotent. A soft-deleted user is also marked as inactive.
        /// Raises a <see cref="UserSoftDeletedDomainEvent"/>.
        /// </remarks>
        public void MarkAsDeleted(Guid? deletedByUserId)
        {
            if (IsDeleted) return;

            IsDeleted = true;
            DeletedAtUtc = DateTime.UtcNow;
            DeletedBy = deletedByUserId;
            IsActive = false; // A deleted user must be inactive.
            SetModificationAudit(deletedByUserId);

            RaiseDomainEvent(new UserSoftDeletedDomainEvent(Id, DeletedAtUtc.Value));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Generates a cryptographically secure, URL-safe random token.
        /// </summary>
        /// <returns>A URL-safe, base64-encoded random string.</returns>
        private static string GenerateUrlSafeToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(tokenBytes)
                .TrimEnd('=') // Remove padding
                .Replace('+', '-') // URL-safe
                .Replace('/', '_'); // URL-safe
        }

        #endregion
    }
}
