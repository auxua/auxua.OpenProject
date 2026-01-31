using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace auxua.OpenProject.Client
{
    public sealed class TimeEntriesApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        public TimeEntriesApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

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
        public sealed class TimeEntriesQuery
        {
            private readonly System.Collections.Generic.List<object> _filters = new();

            public static TimeEntriesQuery ForProject(int projectId)
            {
                var q = new TimeEntriesQuery();
                q._filters.Add(new { project = new { @operator = "=", values = new[] { projectId.ToString() } } });
                return q;
            }

            public TimeEntriesQuery ForWorkPackage(int workPackageId)
            {
                _filters.Add(new { workPackage = new { @operator = "=", values = new[] { workPackageId.ToString() } } });
                return this;
            }

            public TimeEntriesQuery ForUserMe()
            {
                _filters.Add(new { user = new { @operator = "=", values = new[] { "me" } } });
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
