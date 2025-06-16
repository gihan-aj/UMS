using Microsoft.EntityFrameworkCore;

namespace UMS.SharedKernel
{
    /// <summary>
    /// Represents a paginated list of items.
    /// </summary>
    /// <typeparam name="T">The type of the items in the list.</typeparam>
    public class PagedList<T>
    {
        /// <summary>
        /// Gets the items for the current page.
        /// </summary>
        public IReadOnlyCollection<T> Items { get; }

        /// <summary>
        /// Gets the current page number.
        /// </summary>
        public int Page { get; }

        /// <summary>
        /// Gets the number of items per page.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Gets the total number of items across all pages.
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Gets a value indicating whether there is a next page.
        /// </summary>
        public bool HasNextPage => Page * PageSize < TotalCount;

        /// <summary>
        /// Gets a value indicating whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage => Page > 1;

        public PagedList(List<T> items, int page, int pageSize, int totalCount)
        {
            Items = items;
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;
        }

        public static async Task<PagedList<T>> CreateAsync(
            IQueryable<T> source, 
            int page, 
            int pageSize, 
            CancellationToken cancellationToken = default)
        {
            // Ensure page and page size are vaild
            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);

            // Get the total count of items. This executes a COUNT query on the database.
            int totalCount = await source.CountAsync(cancellationToken);

            // Get the items for the specific page
            // This executes a SELECT query with OFFSET and FETCH (or equivalent) on the database
            var items = await source
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedList<T>(items, page, pageSize, totalCount);
        }
    }
}
