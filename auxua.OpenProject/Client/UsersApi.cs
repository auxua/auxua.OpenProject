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
    /// <summary>
    /// Client for interacting with OpenProject users
    /// Provides methods to list users, retrieve single users and fetch the current authenticated user
    /// </summary>
    public sealed class UsersApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        /// <summary>
        /// Create a new <see cref="UsersApi"/> instance.
        /// </summary>
        /// <param name="http">The <see cref="HttpClient"/> used to send requests to the OpenProject API.</param>
        /// <param name="auth">Optional authentication provider that will apply authentication headers to requests.</param>
        public UsersApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

        /// <summary>
        /// Retrieve a page of users from the OpenProject API
        /// </summary>
        /// <param name="pageSize">Number of users per page. Defaults to 100.</param>
        /// <param name="offset">Page offset to retrieve. Defaults to 1.</param>
        /// <param name="query">Optional <see cref="UsersQuery"/> to filter the results.</param>
        /// <returns>A task that resolves to a <see cref="HalCollection{User}"/> containing the users for the requested page.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code. The exception contains the HTTP status code and response body.</exception>
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
        /// Retrieve a single user by identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the user to retrieve.</param>
        /// <returns>A task that resolves to the requested <see cref="User"/>. If deserialization fails an empty <see cref="User"/> is returned.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code.</exception>
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
        /// Retrieve information about the current authenticated user ("me").
        /// </summary>
        /// <returns>A task that resolves to the current <see cref="User"/>. If the endpoint is not available an <see cref="ApiException"/> will be thrown.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code or the endpoint is not supported.</exception>
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

        /// <summary>
        /// Fetch all users by iterating the paginated endpoint using the pagination helper
        /// </summary>
        /// <param name="pageSize">Number of users to request per page when fetching. Defaults to 100.</param>
        /// <param name="query">Optional <see cref="UsersQuery"/> to filter the results.</param>
        /// <returns>A task that resolves to a list containing all users that match the optional query.</returns>
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
        /// <summary>
        /// Helper to build URL-encoded filter queries for the users listing endpoint
        /// Provides fluent methods to append filters which are serialized to the OpenProject filter JSON format.
        /// </summary>
        public sealed class UsersQuery
        {
            private readonly List<object> _filters = new();

            // Beispiele – hängt davon ab, welche Filter OpenProject für users aktuell anbietet.
            // (Ich lasse es absichtlich minimal und erweiterbar.)

            /// <summary>
            /// Add a filter that matches users whose name contains the given text
            /// Note: Filter names are endpoint-specific; ensure your OpenProject instance supports the "name" filter.
            /// </summary>
            /// <param name="text">The substring to match within the user's name.</param>
            /// <returns>The same <see cref="UsersQuery"/> instance for chaining.</returns>
            public UsersQuery NameContains(string text)
            {
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

            /// <summary>
            /// Build the URL-encoded JSON filter string to append to the users endpoin
            /// </summary>
            /// <returns>A URL-encoded JSON representation of the configured filters.</returns>
            public string Build()
            {
                var json = JsonConvert.SerializeObject(_filters);
                return HttpUtility.UrlEncode(json);
            }
        }
    }
}
