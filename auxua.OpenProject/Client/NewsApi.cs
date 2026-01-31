using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using auxua.OpenProject.Model.auxua.OpenProject.Model;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace auxua.OpenProject.Client
{
    /// <summary>
    /// Client for interacting with OpenProject news
    /// Provides methods to list, retrieve, create, update and delete news items.
    /// </summary>
    public sealed class NewsApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        /// <summary>
        /// Create a new <see cref="NewsApi"/> instance.
        /// </summary>
        /// <param name="http">The HTTP client used to send requests to the OpenProject API.</param>
        /// <param name="auth">Optional authentication provider that will apply authentication headers to requests.</param>
        public NewsApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

        /// <summary>
        /// List news items (system-wide aggregate).
        /// Uses pagination parameters <paramref name="pageSize"/> and <paramref name="offset"/>.
        /// </summary>
        /// <param name="pageSize">Number of items per page. Defaults to 100.</param>
        /// <param name="offset">Page offset to retrieve. Defaults to 1.</param>
        /// <returns>A task that resolves to a <see cref="HalCollection{News}"/> containing the news items for the requested page.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code.</exception>
        public async Task<HalCollection<News>> GetNewsAsync(int pageSize = 100, int offset = 1)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/news?pageSize={pageSize}&offset={offset}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            var res = JsonConvert.DeserializeObject<HalCollection<News>>(body) ?? new HalCollection<News>();
            foreach (var item in res.Elements)
            {
                item.PostProcess();
            }
            return res;
        }

        /// <summary>
        /// List news items with optional query filters.
        /// </summary>
        /// <param name="query">Optional <see cref="NewsQuery"/> containing filter criteria.</param>
        /// <param name="pageSize">Number of items per page. Defaults to 100.</param>
        /// <param name="offset">Page offset to retrieve. Defaults to 1.</param>
        /// <returns>A task that resolves to a <see cref="HalCollection{News}"/> containing the news items for the requested page.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code.</exception>
        public async Task<HalCollection<News>> GetNewsAsync(NewsQuery? query, int pageSize = 100, int offset = 1)
        {
            var url = $"api/v3/news?pageSize={pageSize}&offset={offset}";
            if (query != null) url += $"&filters={query.Build()}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            var res = JsonConvert.DeserializeObject<HalCollection<News>>(body) ?? new HalCollection<News>();
            foreach (var item in res.Elements)
            {
                item.PostProcess();
            }
            return res;
        }

        /// <summary>
        /// Fetch all news items by iterating the paginated endpoint.
        /// </summary>
        /// <param name="pageSize">Number of items to request per page when fetching. Defaults to 100.</param>
        /// <returns>A task that resolves to a list containing all news items.</returns>
        public Task<System.Collections.Generic.List<News>> GetAllNewsAsync(int pageSize = 100)
        {
            return PaginationHelper.FetchAllAsync<News>(
                (page, ps) => GetNewsAsync(ps, page),
                pageSize: pageSize,
                startPage: 1
            );
        }

        /// <summary>
        /// Fetch all news items that match the provided query by iterating the paginated endpoint.
        /// </summary>
        /// <param name="query">Optional <see cref="NewsQuery"/> used to filter results.</param>
        /// <param name="pageSize">Number of items to request per page when fetching. Defaults to 100.</param>
        /// <returns>A task that resolves to a list containing all matching news items.</returns>
        public Task<System.Collections.Generic.List<News>> GetAllNewsAsync(NewsQuery? query, int pageSize = 100)
        {
            return PaginationHelper.FetchAllAsync<News>(
                (page, ps) => GetNewsAsync(query, ps, page),
                pageSize: pageSize,
                startPage: 1
            );
        }

        /// <summary>
        /// Retrieve a single news item by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the news item to retrieve.</param>
        /// <returns>A task that resolves to the requested <see cref="News"/>. If deserialization fails an empty <see cref="News"/> is returned.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code.</exception>
        public async Task<News> GetNewsByIdAsync(int id)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/news/{id}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            var res = JsonConvert.DeserializeObject<News>(body) ?? new News();
            res.PostProcess();
            return res;
        }

        /// <summary>
        /// Create a news item in the specified project.
        /// Requires administrative rights or the "Manage news" permission in the target project.
        /// </summary>
        /// <param name="projectId">The id of the project the news should belong to.</param>
        /// <param name="title">The news title.</param>
        /// <param name="summary">Optional summary text for the news.</param>
        /// <param name="descriptionMarkdown">The description in Markdown format.</param>
        /// <returns>A task that resolves to the created <see cref="News"/> instance.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code.</exception>
        public async Task<News> CreateNewsAsync(int projectId, string title, string? summary, string descriptionMarkdown)
        {
            var payload = new
            {
                title,
                summary,
                description = new { format = "markdown", raw = descriptionMarkdown },
                _links = new
                {
                    project = new { href = $"/api/v3/projects/{projectId}" }
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v3/news");
            req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<News>(body) ?? new News();
        }

        /// <summary>
        /// Update an existing news item. Only provided fields will be changed
        /// </summary>
        /// <param name="id">The identifier of the news item to update.</param>
        /// <param name="title">Optional new title.</param>
        /// <param name="summary">Optional new summary.</param>
        /// <param name="descriptionMarkdown">Optional new description in Markdown format.</param>
        /// <returns>A task that resolves to the updated <see cref="News"/> instance.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code.</exception>
        public async Task<News> UpdateNewsAsync(int id, string? title = null, string? summary = null, string? descriptionMarkdown = null)
        {
            var payload = new System.Collections.Generic.Dictionary<string, object>();

            if (title != null) payload["title"] = title;
            if (summary != null) payload["summary"] = summary;
            if (descriptionMarkdown != null)
                payload["description"] = new { format = "markdown", raw = descriptionMarkdown };

            using var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"api/v3/news/{id}");
            req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<News>(body) ?? new News();
        }

        /// <summary>
        /// Delete a news item by its identifier.
        /// </summary>
        /// <param name="id">The identifier of the news item to delete.</param>
        /// <returns>A task that completes when the delete operation has finished.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code.</exception>
        public async Task DeleteNewsAsync(int id)
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"api/v3/news/{id}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);
        }

        // ----------------------------
        // Query builder (optional)
        // ----------------------------
        /// <summary>
        /// Helper to build URL-encoded filter queries for news listing endpoints
        /// </summary>
        public sealed class NewsQuery
        {
            private readonly System.Collections.Generic.List<object> _filters = new();

            /// <summary>
            /// Create a query that filters news for a specific project.
            /// </summary>
            /// <param name="projectId">The id of the project to filter by.</param>
            /// <returns>A <see cref="NewsQuery"/> that filters results to the specified project.</returns>
            public static NewsQuery ForProject(int projectId)
            {
                var q = new NewsQuery();
                q._filters.Add(new
                {
                    project = new
                    {
                        @operator = "=",
                        values = new[] { projectId.ToString() }
                    }
                });
                return q;
            }

            /// <summary>
            /// Build the URL encoded filter string to append to the news endpoint
            /// </summary>
            /// <returns>A URL-encoded JSON representation of the configured filters</returns>
            public string Build()
            {
                var json = JsonConvert.SerializeObject(_filters);
                return HttpUtility.UrlEncode(json);
            }
        }
    }
}
