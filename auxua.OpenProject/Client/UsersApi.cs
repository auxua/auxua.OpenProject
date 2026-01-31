using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace auxua.OpenProject.Client
{
    public sealed class UsersApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        public UsersApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

        /// <summary>
        /// List users (paginated).
        /// GET /api/v3/users?pageSize={pageSize}&offset={offset}
        /// Optional: add filters via UsersQuery (same JSON filter mechanism).
        /// </summary>
        public async Task<HalCollection<User>> GetUsersAsync(int pageSize = 100, int offset = 1, UsersQuery? query = null)
        {
            var url = $"api/v3/users?pageSize={pageSize}&offset={offset}";

            if (query != null)
                url += $"&filters={query.Build()}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<User>>(body)
                   ?? new HalCollection<User>();
        }

        /// <summary>
        /// Get a single user by id.
        /// GET /api/v3/users/{id}
        /// </summary>
        public async Task<User> GetUserByIdAsync(int id)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/users/{id}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<User>(body) ?? new User();
        }

        /// <summary>
        /// Get current user ("me").
        /// Many installations support /api/v3/users/me.
        /// If not, we fallback to parsing it from /api/v3/my_preferences (link to user) later if needed.
        /// </summary>
        public async Task<User> GetMeAsync()
        {
            // 1) Try the common endpoint
            using (var req = new HttpRequestMessage(HttpMethod.Get, "api/v3/users/me"))
            {
                _auth?.Apply(req);

                var resp = await _http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<User>(body) ?? new User();

                // If endpoint not supported / forbidden, surface a clear error
                // (Fallback can be added later via /api/v3/my_preferences which is documented.)
                throw new ApiException(resp.StatusCode, body);
            }
        }

        public Task<List<User>> GetAllUsersAsync(int pageSize = 100, UsersQuery? query = null)
        {
            return PaginationHelper.FetchAllAsync<User>(
                (page, ps) => GetUsersAsync(ps, page, query),
                pageSize: pageSize,
                startPage: 1
            );
        }

        // ----------------------------
        // Query builder (optional)
        // ----------------------------
        public sealed class UsersQuery
        {
            private readonly List<object> _filters = new();

            // Beispiele – hängt davon ab, welche Filter OpenProject für users aktuell anbietet.
            // (Ich lasse es absichtlich minimal und erweiterbar.)

            public UsersQuery NameContains(string text)
            {
                // Achtung: Filter-Namen sind endpoint-spezifisch.
                // Wenn deine Instanz hier "name" unterstützt, kannst du so filtern.
                _filters.Add(new
                {
                    name = new
                    {
                        @operator = "~",
                        values = new[] { text }
                    }
                });
                return this;
            }

            public string Build()
            {
                var json = JsonConvert.SerializeObject(_filters);
                return HttpUtility.UrlEncode(json);
            }
        }
    }
}
