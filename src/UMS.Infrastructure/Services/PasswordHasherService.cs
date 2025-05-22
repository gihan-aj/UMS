using UMS.Application.Abstractions.Services;
using BCryptNet = BCrypt.Net.BCrypt; // Alias to avoid naming conflicts if any

namespace UMS.Infrastructure.Services
{
    public class PasswordHasherService : IPasswordHasherService
    {
        public string HashPassword(string password)
        {
            // BCrypt.HashPassword automatically generates a salt and includes it in the hash string.
            // The work factor (cost) can be adjusted. Default is 10-12. Higher is more secure but slower.
            return BCryptNet.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                // BCrypt.Verify takes the plain text password and the full hash string (which contains the salt).
                return BCryptNet.Verify(password, hashedPassword);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // This can happen if the hashedPassword string is not a valid BCrypt hash (e.g., malformed or wrong version).
                // Log this error for investigation.
                // For security, treat as a verification failure.
                return false;
            }
            // Other exceptions from BCrypt.Verify are unlikely for typical input but could be caught if necessary.
        }
    }
}
