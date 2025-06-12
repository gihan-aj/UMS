using System;

namespace UMS.Infrastructure.Settings
{
    /// <summary>
    /// Configuration settings for the background cleanup jobs.
    /// </summary>
    public class CleanupSettings
    {
        public const string SectionName = "CleanupSettings";

        /// <summary>
        /// The interval at which the cleanup job runs.
        /// </summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromHours(24); // Default to run once every 24 hours

        /// <summary>
        /// The retention period for revoked or expired refresh tokens before they are purged.
        /// </summary>
        public TimeSpan TokenRetentionPeroid { get; set; } = TimeSpan.FromDays(30); // Default to keep for 30 days
    }
}
