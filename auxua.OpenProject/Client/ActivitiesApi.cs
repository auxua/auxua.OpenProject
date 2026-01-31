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
    /// <summary>
    /// Client for working with OpenProject activities.
    /// Provides methods to retrieve single activities and collections of activities related to work packages.
    /// </summary>
    public sealed class ActivitiesApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;
        //private readonly OpenProjectClient _client;

        /// <summary>
        /// Initialize a new instance of <see cref="ActivitiesApi"/>.
        /// </summary>
        /// <param name="http">The <see cref="HttpClient"/> used to send HTTP requests to the OpenProject API.</param>
        /// <param name="auth">Optional authentication provider that will apply authentication headers to requests.</param>
        public ActivitiesApi(HttpClient http, IAuthProvider? auth)
        //public ActivitiesApi(OpenProjectClient client)
        {
            _http = http;
            _auth = auth;
        }

        /// <summary>
        /// Retrieve a single activity by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the activity to retrieve.</param>
        /// <returns>A task that resolves to the <see cref="Activity"/> instance. If the response body cannot be deserialized a new empty <see cref="Activity"/> is returned.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code. The exception contains the HTTP status code and response body.</exception>
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
        /// Retrieve activities associated with a specific work package.
        /// </summary>
        /// <param name="wp">The <see cref="WorkPackage"/> whose activities should be fetched. The work package must contain a HAL link named "activities".</param>
        /// <param name="pageSize">The number of items to request per page. Defaults to 100.</param>
        /// <param name="page">The page offset to request. Defaults to 1.</param>
        /// <returns>A task that resolves to a <see cref="HalCollection{Activity}"/> containing the activities for the work package. If the response body cannot be deserialized a new empty collection is returned.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the provided work package does not contain an "activities" link.</exception>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code. The exception contains the HTTP status code and response body.</exception>
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
