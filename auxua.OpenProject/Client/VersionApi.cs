using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace auxua.OpenProject.Client
{
    /// <summary>
    /// Client for interacting with OpenProject versions
    /// Provides methods to list, retrieve, create, update and delete versions
    /// </summary>
    public sealed class VersionsApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        /// <summary>
        /// Create a new <see cref="VersionsApi"/> instance
        /// </summary>
        /// <param name="http">The <see cref="HttpClient"/> used to send requests to the OpenProject API.</param>
        /// <param name="auth">Optional authentication provider that will apply authentication headers to requests.</param>
        public VersionsApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

        /// <summary>
        /// Retrieve a page of versions from the OpenProject API
        /// </summary>
        /// <param name="pageSize">Number of versions per page. Defaults to 50.</param>
        /// <param name="offset">Page offset to retrieve. Defaults to 1.</param>
        /// <returns>A task that resolves to a <see cref="HalCollection{Version}"/> containing the versions for the requested page.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code. The exception contains the HTTP status code and response body.</exception>
        public async Task<HalCollection<Version>> GetVersionsAsync(int pageSize = 50, int offset = 1)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/versions?pageSize={pageSize}&offset={offset}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<Version>>(body) ?? new HalCollection<Version>();
        }

        /// <summary>
        /// Retrieve versions for a specific workspace
        /// </summary>
        /// <param name="workspaceId">The id of the workspace to list versions for.</param>
        /// <param name="pageSize">Number of versions per page. Defaults to 50.</param>
        /// <param name="offset">Page offset to retrieve. Defaults to 1.</param>
        /// <returns>A task that resolves to a <see cref="HalCollection{Version}"/> containing the versions for the workspace.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code. The exception contains the HTTP status code and response body.</exception>
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
        /// <summary>
        /// Retrieve versions for a specific project (deprecated endpoint)
        /// Use workspace-based endpoints if available
        /// </summary>
        /// <param name="projectId">The id of the project to list versions for.</param>
        /// <param name="pageSize">Number of versions per page. Defaults to 50.</param>
        /// <param name="offset">Page offset to retrieve. Defaults to 1.</param>
        /// <returns>A task that resolves to a <see cref="HalCollection{Version}"/> containing the versions for the project.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code. The exception contains the HTTP status code and response body.</exception>
        public async Task<HalCollection<Version>> GetVersionsForProjectAsync(int projectId, int pageSize = 50, int offset = 1)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/projects/{projectId}/versions?pageSize={pageSize}&offset={offset}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<Version>>(body) ?? new HalCollection<Version>();
        }

        /// <summary>
        /// Retrieve a single version by its identifier
        /// </summary>
        /// <param name="id">The unique identifier of the version to retrieve.</param>
        /// <returns>A task that resolves to the requested <see cref="Version"/>. If deserialization fails an empty <see cref="Version"/> is returned.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code.</exception>
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
        /// <summary>
        /// Validate a create payload against the server-side form endpoint and return the form response.
        /// The response is HAL+JSON and may be used to inspect validation messages before actually creating the resource.
        /// </summary>
        /// <param name="payload">The payload to validate.</param>
        /// <returns>A task that resolves to the raw response body (HAL+JSON) as string.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code for the form request.</exception>
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

        /// <summary>
        /// Create a new version in the specified workspace
        /// </summary>
        /// <param name="workspaceId">The id of the workspace the version belongs to.</param>
        /// <param name="name">The name of the version.</param>
        /// <param name="descriptionMarkdown">Optional description in Markdown format.</param>
        /// <param name="startDate">Optional start date.</param>
        /// <param name="endDate">Optional end date.</param>
        /// <returns>A task that resolves to the created <see cref="Version"/> instance.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code during validation or creation.</exception>
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
                    definingWorkspace = new { href = $"/api/v3/workspaces/{workspaceId}" }
                }
            };

            await GetCreateFormAsync(payload);

            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v3/versions");
            req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<Version>(body) ?? new Version();
        }

        /// <summary>
        /// Update an existing version. Only provided fields will be changed
        /// </summary>
        /// <param name="id">The identifier of the version to update.</param>
        /// <param name="name">Optional new name.</param>
        /// <param name="descriptionMarkdown">Optional new description in Markdown format.</param>
        /// <param name="startDate">Optional new start date.</param>
        /// <param name="endDate">Optional new end date.</param>
        /// <param name="status">Optional status value.</param>
        /// <param name="sharing">Optional sharing setting.</param>
        /// <returns>A task that resolves to the updated <see cref="Version"/> instance.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code during validation or update.</exception>
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

        /// <summary>
        /// Delete a version by its identifier
        /// </summary>
        /// <param name="id">The identifier of the version to delete.</param>
        /// <returns>A task that completes when the delete operation has finished.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code.</exception>
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

