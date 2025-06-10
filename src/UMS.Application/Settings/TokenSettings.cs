namespace UMS.Application.Settings
{
    /// <summary>
    /// Configuration settings for various token lifespans.
    /// Defines the shape of settings required by the Application layer.
    /// </summary>
    public class TokenSettings
    {
        public const string SectionName = "TokenSettings";

        public int ActivationTokenExpiryHours { get; set; } = 24; // Default to 24 hours
        public int PasswordResetTokenExpiryMinutes { get; set; } = 30; // Default to 30 minutes
        public int RefreshTokenExpiryDays { get; set; } = 7; // Default to 7 days
    }
}
