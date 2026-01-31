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
    public sealed class RelationsApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        public RelationsApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

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
