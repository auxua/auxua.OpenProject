using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using auxua.OpenProject.REST;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace auxua.OpenProject.Client
{
    public sealed class ActivitiesApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;
        //private readonly OpenProjectClient _client;

        public ActivitiesApi(HttpClient http, IAuthProvider? auth)
        //public ActivitiesApi(OpenProjectClient client)
        {
            _http = http;
            _auth = auth;
        }

        /// <summary>
        /// Call one Activity by its ID
        /// </summary>
        /// <param name="id">Activity ID</param>
        public async Task<Activity> GetActivityByIdAsync(int id)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/activities/{id}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<Activity>(body) ?? new Activity();
        }



        /// <summary>
        /// Get Activities for a given WorkPackage
        /// </summary>
        /// <param name="wp">the Workpackage to query</param>
        /// <param name="pageSize"></param>
        /// <param name="page"></param>
        public async Task<HalCollection<Activity>> GetActivitiesForWorkPackageAsync(WorkPackage wp, int pageSize = 100, int page = 1)
        {
            // Get correct href for activities link
            var href = wp.Links["activities"].First().First().ToString(); // /api/v3/work_packages/123/activities
            // Strip leading URL part
            if (!href.StartsWith("/api/v3/"))
                href = href.Substring(href.IndexOf("/api/v3/", StringComparison.Ordinal));
            if (string.IsNullOrWhiteSpace(href))
                throw new InvalidOperationException("WorkPackage has no acitivities");

            // pagination params 
            var url =  $"{href}?pageSize={pageSize}&offset={page}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url.TrimStart('/'));
            _auth?.Apply(req);

            

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<Activity>>(body) ?? new HalCollection<Activity>();
        }
    }

}
