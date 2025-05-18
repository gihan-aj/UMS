namespace UMS.Domain.Users
{
    public class User //This is the aggregate root for the User context. Consider adding a base class like Entity or AggregateRoot later
    {
        public Guid Id { get; private set; } // Private set enforces creation logic

        public string Email { get; private set; }

        public string? FirstName { get; private set; }

        public string? LastName { get; private set; }

        public string PasswordHash { get; private set; } // Will be set during registration

        public DateTime CreatedAtUtc { get; private set; }

        // Private constructor for ORM/Persistence frameworks
        private User() { }

        /// <summary>
        /// Factory method or constructor to create a new user.
        /// Enforces required fields and potentially initial validation/logic.
        /// </summary>
        /// <param name="email">The user's email.</param>
        /// <param name="passwordHash">The pre-hashed password.</param>
        /// <param name="firstName">Optional first name.</param>
        /// <param name="lastName">Optional last name.</param>
        /// <returns>A new User instance.</returns>
        /// <exception cref="ArgumentException">Thrown if required arguments are invalid.</exception>
        public static User Create(string email, string passwordHash, string? firstName = null, string? lastName = null)
        {
            // Basic validation - more robust validation can be added
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be empty.", nameof(email));
            }
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));
            }
            // Add email format validation?

            var user = new User
            {
                Id = Guid.NewGuid(), // Generate new ID
                Email = email.ToLowerInvariant(), // Store email consistently
                PasswordHash = passwordHash,
                FirstName = firstName,
                LastName = lastName,
                CreatedAtUtc = DateTime.UtcNow
            };

            // TODO: Raise a Domain Event (e.g., UserCreatedDomainEvent) here later

            return user;
        }

        // Potential future methods:
        // public void ChangePassword(string newPasswordHash) { ... }
        // public void UpdateProfile(string? firstName, string? lastName) { ... }
        // private void RaiseDomainEvent(IDomainEvent domainEvent) { ... }
    }
}
