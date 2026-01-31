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
    public sealed class NewsApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        public NewsApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

        /// <summary>
        /// List news (system-wide aggregate). Pagination uses pageSize + offset.
        /// GET /api/v3/news
        /// </summary>
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
        /// Optional: list news with filters (same filter mechanism as elsewhere).
        /// </summary>
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

        public Task<System.Collections.Generic.List<News>> GetAllNewsAsync(int pageSize = 100)
        {
            return PaginationHelper.FetchAllAsync<News>(
                (page, ps) => GetNewsAsync(ps, page),
                pageSize: pageSize,
                startPage: 1
            );
        }

        public Task<System.Collections.Generic.List<News>> GetAllNewsAsync(NewsQuery? query, int pageSize = 100)
        {
            return PaginationHelper.FetchAllAsync<News>(
                (page, ps) => GetNewsAsync(query, ps, page),
                pageSize: pageSize,
                startPage: 1
            );
        }

        /// <summary>
        /// View single news.
        /// GET /api/v3/news/{id}
        /// </summary>
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
        /// Create news. Requires admin or "Manage news" permission in target project.
        /// POST /api/v3/news
        /// </summary>
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
        /// Update news.
        /// PATCH /api/v3/news/{id}
        /// </summary>
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
        /// Delete news.
        /// DELETE /api/v3/news/{id}
        /// </summary>
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
        public sealed class NewsQuery
        {
            private readonly System.Collections.Generic.List<object> _filters = new();

            // Beispiel: nach Projekt filtern (falls du das brauchst)
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

            public string Build()
            {
                var json = JsonConvert.SerializeObject(_filters);
                return HttpUtility.UrlEncode(json);
            }
        }
    }
}
