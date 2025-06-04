namespace UMS.Infrastructure.Authentication.Settings
{
    /// <summary>
    /// Configuration settings for JWT generation.
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";  // For binding from appsettings.json

        /// <summary>
        /// The secret key used to sign the JWT. Must be strong and kept secure.
        /// Should be at least 32 characters long for HMACSHA256.
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// The issuer of the JWT (e.g., your application's domain).
        /// </summary>
        public string Issuer {  get; set; } = string.Empty;

        /// <summary>
        /// The audience of the JWT (e.g., the client application or API).
        /// </summary>
        public string Audience {  get; set; } = string.Empty;

        /// <summary>
        /// The duration in minutes for which the JWT will be valid.
        /// </summary>
        public int ExpiryMinutes { get; set; } = 60;
    }
}
