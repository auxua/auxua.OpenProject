using auxua.OpenProject.Model;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace auxua.OpenProject.Client
{
    public static class PaginationHelper
    {
        /// <summary>
        /// Fetches all elements from a paginated OpenProject HAL collection by iterating pages.
        /// Assumes OpenProject's "offset" is a PAGE NUMBER (1-based) as documented.
        /// </summary>
        public static async Task<List<T>> FetchAllAsync<T>(
            Func<int, int, Task<HalCollection<T>>> fetchPageAsync,
            int pageSize = 100,
            int startPage = 1,
            int? maxPages = null,
            CancellationToken ct = default)
        {
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));
            if (startPage <= 0) throw new ArgumentOutOfRangeException(nameof(startPage));

            var all = new List<T>();
            var page = startPage;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var resp = await fetchPageAsync(page, pageSize).ConfigureAwait(false);

                var elements = resp.Elements ?? new List<T>();
                var returned = elements.Count;

                // NEW: empty page => stop (prevents endless loops)
                if (returned == 0)
                {
                    
                    
                    break;
                }

                all.AddRange(elements);

                // Stop if total is reliable
                if (resp.Total > 0 && all.Count >= resp.Total)
                    break;

                // NEW: use effective page size (server may cap pageSize)
                var effectivePageSize = resp.PageSize > 0 ? resp.PageSize : pageSize;

                // Last page if fewer returned than page size
                if (returned < effectivePageSize)
                    break;

                if (maxPages.HasValue && (page - startPage + 1) >= maxPages.Value)
                    break;

                page++;
            }

            return all;
        }
    }
}