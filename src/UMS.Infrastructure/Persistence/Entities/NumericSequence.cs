namespace UMS.Infrastructure.Persistence.Entities
{
    /// <summary>
    /// Represents a sequence counter for generating simple numeric primary keys.
    /// </summary>
    public class NumericSequence
    {
        public string SequenceName { get; set; } = string.Empty;
        public long LastValue { get; set; }
    }
}
