namespace UMS.SharedKernel
{
    /// <summary>
    /// Represents a query with parameters for pagination, sorting, and filtering.
    /// </summary>
    public class PaginationQuery
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public string? SortColumn { get; set; }

        public string? SortOrder { get; set; } = "asc";

        public List<Filter>? Filters { get; set; }
    }
}
