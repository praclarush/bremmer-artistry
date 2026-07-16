using System;
using System.Collections.Generic;
using System.Linq;

namespace PotteryJournal.SharedKernel.Core
{
    /// <summary>
    /// Response returned by infrastructure handler methods that return a paged collection.
    /// </summary>
    /// <typeparam name="T">The type of item in the paged collection.</typeparam>
    public class PagedHandlerResponse<T> : HandlerResponse
    {
        private const int DefaultPageSize = 25;
        private const int DefaultPageNumber = 1;

        /// <summary>
        /// Initializes an empty response: no records, page 1, page size 25.
        /// </summary>
        public PagedHandlerResponse()
        {
            Data = new List<T>();
            TotalRecords = 0;
            PageNumber = DefaultPageNumber;
            PageSize = DefaultPageSize;
        }

        /// <summary>
        /// Initializes a response with the given page of results.
        /// </summary>
        /// <param name="data">The items on the current page.</param>
        /// <param name="totalRecords">The total number of records across all pages.</param>
        /// <param name="pageSize">The number of records per page.</param>
        /// <param name="pageNumber">The current page number, 1-based.</param>
        public PagedHandlerResponse(IEnumerable<T> data, int totalRecords, int pageSize, int pageNumber)
        {
            Data = data.ToList();
            TotalRecords = totalRecords;
            PageSize = pageSize;
            PageNumber = pageNumber;
        }

        /// <summary>
        /// Gets or sets the items on the current page.
        /// </summary>
        public IReadOnlyList<T> Data { get; set; }

        /// <summary>
        /// Gets or sets the total number of records across all pages.
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Gets or sets the number of records per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the current page number, 1-based.
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Gets the total number of pages given <see cref="TotalRecords"/> and <see cref="PageSize"/>.
        /// </summary>
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRecords / (double)PageSize);
    }
}
