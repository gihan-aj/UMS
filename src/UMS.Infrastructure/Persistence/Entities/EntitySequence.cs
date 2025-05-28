using System;

namespace UMS.Infrastructure.Persistence.Entities
{
    /// <summary>
    /// Represents a sequence counter for generating human-readable entity codes.
    /// Configuration is handled via IEntityTypeConfiguration.
    /// </summary>
    public class EntitySequence
    {
        /// <summary>
        /// The prefix for the entity type (e.g., "USR", "ORD").
        /// Part of the composite primary key.
        /// </summary>
        public string EntityTypePrefix { get; set; } = string.Empty;

        /// <summary>
        /// The date for which this sequence applies (resets daily).
        /// Part of the composite primary key.
        /// </summary>
        public DateTime SequenceDate { get; set; }

        /// <summary>
        /// The last sequence value used for this prefix and date.
        /// </summary>
        public int LastValue { get; set; }
    }
}
