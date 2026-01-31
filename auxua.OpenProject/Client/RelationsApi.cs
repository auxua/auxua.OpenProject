using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace auxua.OpenProject.Client
{
    /// <summary>
    /// Client for working with work package relations in the OpenProject API
    /// Provides methods to list, create and retrieve relations associated with work packages
    /// </summary>
    public sealed class RelationsApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        /// <summary>
        /// Create a new <see cref="RelationsApi"/> instance
        /// </summary>
        /// <param name="http">The <see cref="HttpClient"/> used to send requests to the OpenProject API.</param>
        /// <param name="auth">Optional authentication provider that will apply authentication headers to requests.</param>
        public RelationsApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

        /// <summary>
        /// Retrieve relations for the specified work package
        /// </summary>
        /// <param name="wp">The <see cref="WorkPackage"/> whose relations should be fetched. The work package must contain a HAL link named "relations".</param>
        /// <param name="pageSize">Number of items per page. Defaults to 100.</param>
        /// <param name="page">Page offset to retrieve. Defaults to 1.</param>
        /// <returns>A task that resolves to a <see cref="HalCollection{Relation}"/> containing the relations for the work package.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the provided work package does not contain a "relations" link.</exception>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code. The exception contains the HTTP status code and response body.</exception>
        public async Task<HalCollection<Relation>> GetRelationsForWorkPackageAsync(WorkPackage wp, int pageSize = 100, int page = 1)
        {
            // Get correct href for relations link
            var href = wp.Links["relations"].First().First().ToString(); // /api/v3/work_packages/123/relations
            // Strip leading URL part
            if (!href.StartsWith("/api/v3/"))
                href = href.Substring(href.IndexOf("/api/v3/", StringComparison.Ordinal));
            if (string.IsNullOrWhiteSpace(href))
                throw new InvalidOperationException("WorkPackage has no relations");
            var url = $"{href}?pageSize={pageSize}&offset={page}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url.TrimStart('/'));
            _auth?.Apply(req);
            var resp = await _http.SendAsync(req);
            // In case of redirection, follow the Location header
            if ((int)resp.StatusCode is >= 300 and < 400 && resp.Headers.Location != null)
            {
                var nextUri = resp.Headers.Location.IsAbsoluteUri
                    ? resp.Headers.Location
                    : new Uri(_http.BaseAddress!, resp.Headers.Location);

                using var req2 = new HttpRequestMessage(HttpMethod.Get, nextUri);
                _auth?.Apply(req2);

                resp = await _http.SendAsync(req2);
            }
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) 
                throw new ApiException(resp.StatusCode, body);
            return JsonConvert.DeserializeObject<HalCollection<Relation>>(body) ?? new HalCollection<Relation>();
        }

        /// <summary>
        /// List relations across the system with optional filters
        /// </summary>
        /// <param name="encodedFilters">Optional URL-encoded filter string to apply to the listing.</param>
        /// <param name="pageSize">Number of items per page. Defaults to 100.</param>
        /// <param name="page">Page offset to retrieve. Defaults to 1.</param>
        /// <returns>A task that resolves to a <see cref="HalCollection{Relation}"/> containing the requested page of relations.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code. The exception contains the HTTP status code and response body.</exception>
        public async Task<HalCollection<Relation>> ListRelationsAsync(string? encodedFilters = null, int pageSize = 100, int page = 1)
        {
            var filterPart = string.IsNullOrWhiteSpace(encodedFilters) ? "" : $"&filters={encodedFilters}";
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/relations?pageSize={pageSize}&offset={page}{filterPart}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<Relation>>(body) ?? new HalCollection<Relation>();
        }

        // Create relation: POST /api/v3/work_packages/{id}/relations
        /// <summary>
        /// Create a relation from one work package to another
        /// </summary>
        /// <param name="fromWorkPackageId">The id of the source work package.</param>
        /// <param name="toWorkPackageId">The id of the target work package.</param>
        /// <param name="type">The relation type (e.g. "follows", "precedes").</param>
        /// <param name="description">Optional description for the relation.</param>
        /// <param name="lag">Optional lag value for the relation.</param>
        /// <returns>A task that resolves to the created <see cref="Relation"/> instance.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code. The exception contains the HTTP status code and response body.</exception>
        public async Task<Relation> CreateRelationAsync(int fromWorkPackageId, int toWorkPackageId, string type, string? description = null, int? lag = null)
        {
            var payload = new
            {
                type,
                description,
                lag,
                _links = new
                {
                    to = new { href = $"/api/v3/work_packages/{toWorkPackageId}" }
                }
            };

            var json = JsonConvert.SerializeObject(payload);

            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v3/work_packages/{fromWorkPackageId}/relations");
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<Relation>(body) ?? new Relation();
        }
    }

}
