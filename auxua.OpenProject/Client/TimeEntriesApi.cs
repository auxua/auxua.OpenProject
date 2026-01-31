using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace auxua.OpenProject.Client
{
    /// <summary>
    /// Client for interacting with OpenProject time entries
    /// Provides methods to list, retrieve and create time entries
    /// </summary>
    public sealed class TimeEntriesApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        /// <summary>
        /// Initialize a new instance of <see cref="TimeEntriesApi"/>.
        /// </summary>
        /// <param name="http">The <see cref="HttpClient"/> used to send requests to the OpenProject API.</param>
        /// <param name="auth">Optional authentication provider that will apply authentication headers to requests.</param>
        public TimeEntriesApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

        /// <summary>
        /// Retrieve a page of time entries with optional filters
        /// </summary>
        /// <param name="query">Optional <see cref="TimeEntriesQuery"/> containing filter criteria.</param>
        /// <param name="pageSize">Number of items per page. Defaults to 100.</param>
        /// <param name="page">Page offset to retrieve. Defaults to 1.</param>
        /// <returns>A task that resolves to a <see cref="HalCollection{TimeEntry}"/> containing the requested page of time entries.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code. The exception contains the HTTP status code and response body.</exception>
        public async Task<HalCollection<TimeEntry>> GetTimeEntriesAsync(TimeEntriesQuery? query = null, int pageSize = 100, int page = 1)
        {
            var url = $"api/v3/time_entries?pageSize={pageSize}&offset={page}";
            if (query != null) url += $"&filters={query.Build()}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<TimeEntry>>(body) ?? new HalCollection<TimeEntry>();
        }

        /// <summary>
        /// Retrieve a single time entry by its identifier
        /// </summary>
        /// <param name="id">The unique identifier of the time entry to retrieve.</param>
        /// <returns>A task that resolves to the requested <see cref="TimeEntry"/>. If deserialization fails an empty <see cref="TimeEntry"/> is returned.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code.</exception>
        public async Task<TimeEntry> GetTimeEntryByIdAsync(int id)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/time_entries/{id}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<TimeEntry>(body) ?? new TimeEntry();
        }


        /// <summary>
        /// Create a new time entry in the specified project and activity
        /// </summary>
        /// <param name="projectId">The id of the project the time entry belongs to.</param>
        /// <param name="activityId">The id of the time entry activity.</param>
        /// <param name="hoursIsoDuration">The duration in ISO 8601 duration format (e.g. "PT1H30M").</param>
        /// <param name="spentOn">The date the time was spent.</param>
        /// <param name="workPackageId">Optional work package id to associate the time entry with.</param>
        /// <param name="commentMarkdown">Optional comment in Markdown format.</param>
        /// <returns>A task that resolves to the created <see cref="TimeEntry"/> instance.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code during validation or creation.</exception>
        public async Task<TimeEntry> CreateTimeEntryAsync(
            int projectId,
            int activityId,
            string hoursIsoDuration,     // z.B. "PT1H30M"
            System.DateTime spentOn,
            int? workPackageId = null,
            string? commentMarkdown = null)
        {
            var payload = new
            {
                hours = hoursIsoDuration,
                spentOn = spentOn.ToString("yyyy-MM-dd"),
                comment = commentMarkdown != null ? new { format = "markdown", raw = commentMarkdown } : null,
                _links = new
                {
                    project = new { href = $"/api/v3/projects/{projectId}" },
                    activity = new { href = $"/api/v3/time_entry_activities/{activityId}" },
                    workPackage = workPackageId.HasValue ? new { href = $"/api/v3/work_packages/{workPackageId.Value}" } : null
                }
            };

            // optional: form call (validiert/writable/allowed)
            // POST /api/v3/time_entries/form
            using (var formReq = new HttpRequestMessage(HttpMethod.Post, "api/v3/time_entries/form"))
            {
                formReq.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                _auth?.Apply(formReq);

                var formResp = await _http.SendAsync(formReq);
                var formBody = await formResp.Content.ReadAsStringAsync();
                if (!formResp.IsSuccessStatusCode)
                    throw new ApiException(formResp.StatusCode, formBody);
            }

            // aactual create
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v3/time_entries");
            req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<TimeEntry>(body) ?? new TimeEntry();
        }

        // Query Builder
        /// <summary>
        /// Helper to build URL-encoded filter queries for time entries endpoints
        /// Supports fluent methods to add filters for project, work package and current user
        /// </summary>
        public sealed class TimeEntriesQuery
        {
            private readonly System.Collections.Generic.List<object> _filters = new();

            /// <summary>
            /// Start a query that filters time entries for a specific project
            /// </summary>
            /// <param name="projectId">The id of the project to filter by.</param>
            /// <returns>A new <see cref="TimeEntriesQuery"/> instance with the project filter applied.</returns>
            public static TimeEntriesQuery ForProject(int projectId)
            {
                var q = new TimeEntriesQuery();
                q._filters.Add(new { project = new { @operator = "=", values = new[] { projectId.ToString() } } });
                return q;
            }

            /// <summary>
            /// Add a filter to the query to restrict results to the given work package
            /// </summary>
            /// <param name="workPackageId">The id of the work package to filter by.</param>
            /// <returns>The same <see cref="TimeEntriesQuery"/> instance for chaining.</returns>
            public TimeEntriesQuery ForWorkPackage(int workPackageId)
            {
                _filters.Add(new { workPackage = new { @operator = "=", values = new[] { workPackageId.ToString() } } });
                return this;
            }

            /// <summary>
            /// Add a filter that limits results to the current authenticated user ("me")
            /// </summary>
            /// <returns>The same <see cref="TimeEntriesQuery"/> instance for chaining.</returns>
            public TimeEntriesQuery ForUserMe()
            {
                _filters.Add(new { user = new { @operator = "=", values = new[] { "me" } } });
                return this;
            }

            /// <summary>
            /// Build the URL encoded filter string to append to the time entries endpoint
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
