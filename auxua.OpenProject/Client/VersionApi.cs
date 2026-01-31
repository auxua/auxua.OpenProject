using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace auxua.OpenProject.Client
{
    public sealed class VersionsApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        public VersionsApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

        public async Task<HalCollection<Version>> GetVersionsAsync(int pageSize = 50, int offset = 1)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/versions?pageSize={pageSize}&offset={offset}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<Version>>(body) ?? new HalCollection<Version>();
        }

        public async Task<HalCollection<Version>> GetVersionsForWorkspaceAsync(int workspaceId, int pageSize = 50, int offset = 1)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/workspaces/{workspaceId}/versions?pageSize={pageSize}&offset={offset}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<Version>>(body) ?? new HalCollection<Version>();
        }

        // Deprecated endpoint (project) – optional fallback:
        public async Task<HalCollection<Version>> GetVersionsForProjectAsync(int projectId, int pageSize = 50, int offset = 1)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/projects/{projectId}/versions?pageSize={pageSize}&offset={offset}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<Version>>(body) ?? new HalCollection<Version>();
        }

        public async Task<Version> GetVersionByIdAsync(int id)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/versions/{id}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<Version>(body) ?? new Version();
        }

        // Optional: Form-validate before create (recommended)
        public async Task<string> GetCreateFormAsync(object payload)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v3/versions/form");
            req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);
            return body; // Form ist HAL+JSON; fürs MVP kannst du es als string lassen
        }

        public async Task<Version> CreateVersionAsync(
            int workspaceId,
            string name,
            string? descriptionMarkdown = null,
            System.DateTime? startDate = null,
            System.DateTime? endDate = null)
        {
            var payload = new
            {
                name,
                description = descriptionMarkdown != null ? new { format = "markdown", raw = descriptionMarkdown } : null,
                startDate = startDate?.ToString("yyyy-MM-dd"),
                endDate = endDate?.ToString("yyyy-MM-dd"),
                _links = new
                {
                    // Versions gehören zu workspaces (projekt-Ära: project)
                    definingWorkspace = new { href = $"/api/v3/workspaces/{workspaceId}" }
                }
            };

            // (optional, aber empfehlenswert)
            await GetCreateFormAsync(payload);

            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v3/versions");
            req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<Version>(body) ?? new Version();
        }

        public async Task<Version> UpdateVersionAsync(
            int id,
            string? name = null,
            string? descriptionMarkdown = null,
            System.DateTime? startDate = null,
            System.DateTime? endDate = null,
            string? status = null,
            string? sharing = null)
        {
            // PATCH payload: nur setzen, was wirklich geändert wird
            var payload = new System.Collections.Generic.Dictionary<string, object>();

            if (name != null) payload["name"] = name;
            if (descriptionMarkdown != null) payload["description"] = new { format = "markdown", raw = descriptionMarkdown };
            if (startDate.HasValue) payload["startDate"] = startDate.Value.ToString("yyyy-MM-dd");
            if (endDate.HasValue) payload["endDate"] = endDate.Value.ToString("yyyy-MM-dd");
            if (status != null) payload["status"] = status;
            if (sharing != null) payload["sharing"] = sharing;

            // Update form exists: POST /api/v3/versions/{id}/form  (optional, recommended)
            using (var formReq = new HttpRequestMessage(HttpMethod.Post, $"api/v3/versions/{id}/form"))
            {
                formReq.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                _auth?.Apply(formReq);

                var formResp = await _http.SendAsync(formReq);
                var formBody = await formResp.Content.ReadAsStringAsync();
                if (!formResp.IsSuccessStatusCode) throw new ApiException(formResp.StatusCode, formBody);
            }

            using var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"api/v3/versions/{id}");
            req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<Version>(body) ?? new Version();
        }

        public async Task DeleteVersionAsync(int id)
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"api/v3/versions/{id}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);
        }
    }
}

