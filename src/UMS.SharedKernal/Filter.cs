namespace UMS.SharedKernel
{
    /// <summary>
    /// Represents a single filter criterion.
    /// </summary>
    public class Filter
    {
        public string ColumnName { get; set; } = string.Empty;

        public string Operator {  get; set; } = "contains";

        public string Value { get; set; } = string.Empty;
    }
}
