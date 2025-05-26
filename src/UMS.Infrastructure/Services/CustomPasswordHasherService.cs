using System;
using System.Security.Cryptography;
using UMS.Application.Abstractions.Services;

namespace UMS.Infrastructure.Services
{
    /// <summary>
    /// Implements password hashing and verification manually using PBKDF2 (Rfc2898DeriveBytes).
    /// IMPORTANT: This is for educational purposes. For production, use robust, well-tested libraries.
    /// </summary>
    public class CustomPasswordHasherService : IPasswordHasherService
    {
        // These parameters should be configurable and chosen carefully.
        // Higher iteration counts increase security but also processing time.
        private const int SaltSize = 16; // 128 bits
        private const int HashSize = 32; // 256 bits (for HMACSHA2565)
        private const int Iterations = 100_000; // Adjust based on performance/security needs
        private static readonly HashAlgorithmName _hashAlgorithmName = HashAlgorithmName.SHA256;
        private const char SaltHashSeparator = '$';

        /// <summary>
        /// Hashes a plain text password using PBKDF2-HMAC-SHA256.
        /// The salt is generated randomly and prepended to the hash.
        /// Format: Base64(salt)$Base64(hash)
        /// </summary>
        /// <param name="password">The plain text password to hash.</param>
        /// <returns>A string containing both the salt and the hash.</returns>
        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            // 1. Generate a cryptographically secure salt
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            // 2. Hash the password using PBKDF2
            // Rfc2898DeriveBytes uses HMACSHA1 by default if not specified in older .NET Framework.
            // In .NET Core / .NET 5+, you can specify the hash algorithm.
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, _hashAlgorithmName);
            byte[] hash = pbkdf2.GetBytes(HashSize);

            // 3. Combine salt and hash for storage
            // Convert to Base64 strings for easy storage
            string saltBase64 = Convert.ToBase64String(salt);
            string hashBase64 = Convert.ToBase64String(hash);

            return $"{saltBase64}{SaltHashSeparator}{hashBase64}";
        }

        /// <summary>
        /// Verifies a plain text password against a stored salt and hash.
        /// </summary>
        /// <param name="password">The plain text password to verify.</param>
        /// <param name="hashedPassword">The string containing the salt and hash,
        /// formatted as Base64(salt)$Base64(hash).</param>
        /// <returns>True if the password matches the hash, false otherwise.</returns>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            if(string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }
            if (string.IsNullOrEmpty(hashedPassword))
            {
                return false;
            }

            string[] parts = hashedPassword.Split(SaltHashSeparator);
            if (parts.Length != 2)
            {
                // Invalid format for the stored hash
                // Log this issue - it might indicate data corruption or an attempt to use an old hash format.
                return false;
            }

            byte[] salt;
            byte[] storedHash;

            try
            {
                salt = Convert.FromBase64String(parts[0]);
                storedHash = Convert.FromBase64String(parts[1]);
            }
            catch (FormatException)
            {
                // Invalid Base64 string
                // Log this issue.
                return false;
            }

            // Ensure the salt and hash sizes match what we expect (optional but good practice)
            if (salt.Length != SaltSize || storedHash.Length != HashSize)
            {
                return false;
            }

            // Hash the incoming password with the stored salt and same parameters
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, _hashAlgorithmName);
            byte[] computedHash = pbkdf2.GetBytes(HashSize);

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
    }
}
