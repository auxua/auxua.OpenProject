using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace auxua.OpenProject.Client
{
    /// <summary>
    /// Client for retrieving time entry activities from the OpenProject API
    /// Provides methods to list time entry activities with pagination
    /// </summary>
    public sealed class TimeEntryActivitiesApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        /// <summary>
        /// Create a new <see cref="TimeEntryActivitiesApi"/> instance
        /// </summary>
        /// <param name="http">The <see cref="HttpClient"/> used to send requests to the OpenProject API.</param>
        /// <param name="auth">Optional authentication provider that will apply authentication headers to requests.</param>
        public TimeEntryActivitiesApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

        /// <summary>
        /// Retrieve a page of time entry activities
        /// </summary>
        /// <param name="pageSize">Number of items per page. Defaults to 100.</param>
        /// <param name="page">Page offset to retrieve. Defaults to 1.</param>
        /// <returns>A task that resolves to a <see cref="HalCollection{TimeEntriesActivity}"/> containing the requested page of activities.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code. The exception contains the HTTP status code and response body.</exception>
        public async Task<HalCollection<TimeEntriesActivity>> GetActivitiesAsync(int pageSize = 100, int page = 1)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/time_entry_activities?pageSize={pageSize}&offset={page}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<TimeEntriesActivity>>(body)
                   ?? new HalCollection<TimeEntriesActivity>();
        }
    }
}
