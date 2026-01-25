using auxua.OpenProject.Model;
using System;
using System.Collections.Generic;
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
            HalCollection<T>? resp = null;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                resp = await fetchPageAsync(page, pageSize).ConfigureAwait(false);

                // Add elements
                var elements = resp.Elements; // assuming your HalCollection<T> exposes typed Elements
                if (elements.Count > 0)
                    all.AddRange(elements);

                // Stop conditions:
                // 1) We reached total (if total is provided reliably)
                if (resp.Total > 0 && all.Count >= resp.Total)
                    break;

                // 2) Fewer than pageSize returned => likely last page
                if (resp.Count > 0 && resp.Count < pageSize)
                    break;

                // 3) Safety guard
                if (maxPages.HasValue && (page - startPage + 1) >= maxPages.Value)
                    break;

                page++;
            }

            return all;
        }
    }
}