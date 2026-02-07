using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using auxua.OpenProject.WriteModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace auxua.OpenProject.Client
{
    /// <summary>
    /// Client for interacting with work packages in the OpenProject API
    /// Provides methods to list, retrieve and enumerate work packages and to import custom field schemas.
    /// </summary>
    public sealed class WorkPackagesApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;
        private readonly CustomFieldRegistry _customFieldRegistry;
        private readonly RelationsApi _rels;


        /// <summary>
        /// Create a new <see cref="WorkPackagesApi"/> instance
        /// </summary>
        /// <param name="http">The <see cref="HttpClient"/> used to send requests to the OpenProject API.</param>
        /// <param name="auth">Optional authentication provider that will apply authentication headers to requests.</param>
        /// <param name="customFieldRegistry">Registry used to import and store custom field schemas discovered in responses.</param>
        public WorkPackagesApi(HttpClient http, 
            IAuthProvider? auth, 
            CustomFieldRegistry customFieldRegistry, 
            RelationsApi rels)
        {
            _http = http;
            _auth = auth;
            _customFieldRegistry = customFieldRegistry;
            _rels = rels;
        }

        /// <summary>
        /// Retrieve a page of work packages with optional filter criteria.
        /// The method will attempt to import any embedded custom field schemas into the provided <see cref="CustomFieldRegistry"/> but will not fail the request if import fails.
        /// </summary>
        /// <param name="query">Optional <see cref="WorkPackageQuery"/> containing filter criteria.</param>
        /// <param name="pageSize">Number of work packages per page. Defaults to 20.</param>
        /// <param name="page">Page offset to retrieve. Defaults to 1.</param>
        /// <returns>A task that resolves to a <see cref="HalCollection{WorkPackage}"/> containing the requested page of work packages.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code. The exception contains the HTTP status code and response body.</exception>
        public async Task<HalCollection<WorkPackage>> GetWorkPackagesAsync(
            WorkPackageQuery? query = null,
            int pageSize = 20,
            int page = 1)
        {
            var url = $"api/v3/work_packages?pageSize={pageSize}&offset={page}";

            if (query != null)
                url += $"&filters={query.Build()}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            // Best-effort: schemas may be embedded in collection responses
            // We do NOT fail the request if schema import fails.
            try
            {
                _customFieldRegistry.ImportFromWorkPackagesCollectionJson(body);
            }
            catch
            {
                // optional: log later
            }

            var res = JsonConvert.DeserializeObject<HalCollection<WorkPackage>>(body)
                   ?? new HalCollection<WorkPackage>();

            foreach (var item in res.Elements)
            {
                item.AddCustomFields(_customFieldRegistry);
            }
            return res;
        }

        /// <summary>
        /// Retrieve a single work package by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the work package to retrieve.</param>
        /// <returns>A task that resolves to the requested <see cref="WorkPackage"/>. If deserialization fails an empty <see cref="WorkPackage"/> is returned.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code.</exception>
        public async Task<WorkPackage> GetWorkPackageByIdAsync(int id)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/work_packages/{id}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<WorkPackage>(body)
                   ?? new WorkPackage();
        }

        /// <summary>
        /// Fetch all work packages by iterating the paginated endpoint using the pagination helper.
        /// </summary>
        /// <param name="pageSize">Number of work packages to request per page when fetching. Defaults to 100.</param>
        /// <returns>A task that resolves to a list containing all work packages.</returns>
        public Task<List<WorkPackage>> GetAllWorkPackagesAsync(int pageSize = 100)
            => GetAllWorkPackagesAsync(query: null, pageSize: pageSize);

        /// <summary>
        /// Fetch all work packages that match the optional query by iterating the paginated endpoint.
        /// </summary>
        /// <param name="query">Optional <see cref="WorkPackageQuery"/> used to filter results.</param>
        /// <param name="pageSize">Number of work packages to request per page when fetching. Defaults to 100.</param>
        /// <returns>A task that resolves to a list containing all matching work packages.</returns>
        public Task<List<WorkPackage>> GetAllWorkPackagesAsync(WorkPackageQuery? query, int pageSize = 100)
        {
            return PaginationHelper.FetchAllAsync<WorkPackage>(
                (page, ps) => GetWorkPackagesAsync(query, ps, page),
                pageSize: pageSize,
                startPage: 1
            );
        }

        /// <summary>
        /// Fetch all work packages for a specific project by using a project-scoped query and iterating the paginated endpoint.
        /// </summary>
        /// <param name="projectId">The id of the project whose work packages should be fetched.</param>
        /// <param name="pageSize">Number of work packages to request per page when fetching. Defaults to 100.</param>
        /// <returns>A task that resolves to a list containing all work packages for the specified project.</returns>
        public Task<List<WorkPackage>> GetAllWorkPackagesForProjectAsync(int projectId, int pageSize = 100)
        {
            var q = WorkPackageQuery.ForProject(projectId);
            return GetAllWorkPackagesAsync(q, pageSize);
        }

        // ----------------------------
        // Query builder
        // ----------------------------
        /// <summary>
        /// Helper to build URL-encoded filter queries for work package endpoints.
        /// Provides fluent methods to append common filters which are serialized to the OpenProject filter JSON format.
        /// </summary>
        public sealed class WorkPackageQuery
        {
            private readonly List<object> _filters = new();

            /// <summary>
            /// Create a query that filters work packages for a specific project.
            /// </summary>
            /// <param name="projectId">The id of the project to filter by.</param>
            /// <returns>A <see cref="WorkPackageQuery"/> that filters results to the specified project.</returns>
            public static WorkPackageQuery ForProject(int projectId)
            {
                var q = new WorkPackageQuery();
                q._filters.Add(new
                {
                    project = new
                    {
                        @operator = "=",
                        values = new[] { projectId.ToString() }
                    }
                });
                return q;
            }

            /// <summary>
            /// Add a filter to the query to restrict results to work packages assigned to the current authenticated user ("me").
            /// </summary>
            /// <returns>The same <see cref="WorkPackageQuery"/> instance for chaining.</returns>
            public WorkPackageQuery AssignedToMe()
            {
                _filters.Add(new
                {
                    assigned_to_id = new
                    {
                        @operator = "=",
                        values = new[] { "me" }
                    }
                });
                return this;
            }

            /// <summary>
            /// Build the URL-encoded JSON filter string to append to work package listing endpoints.
            /// </summary>
            /// <returns>A URL-encoded JSON representation of the configured filters.</returns>
            public string Build()
            {
                var json = JsonConvert.SerializeObject(_filters);
                return HttpUtility.UrlEncode(json);
            }
        }


        public async Task<WorkPackage> CreateWorkPackageAsync(WorkPackageChangeSet cs)
        {
            var payload = WorkPackageChangeSet.BuildWorkPackagePayload(cs);
            var json = payload.ToString(Newtonsoft.Json.Formatting.None);

            // 1) form validate (optional)
            using (var formReq = new HttpRequestMessage(HttpMethod.Post, "api/v3/work_packages/form"))
            {
                formReq.Content = new StringContent(json, Encoding.UTF8, "application/json");
                _auth?.Apply(formReq);

                var formResp = await _http.SendAsync(formReq);
                var formBody = await formResp.Content.ReadAsStringAsync();
                if (!formResp.IsSuccessStatusCode)
                    throw new ApiException(formResp.StatusCode, formBody);
            }

            // 2) create
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v3/work_packages");
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            var wp = JsonConvert.DeserializeObject<WorkPackage>(body) ?? new WorkPackage();
            wp.AddCustomFields(_customFieldRegistry);
            return wp;
        }

        public async Task<WorkPackage> UpdateWorkPackageAsync(int id, WorkPackageChangeSet cs, int? lockVersion = null)
        {
            var payload = WorkPackageChangeSet.BuildWorkPackagePayload(cs);

            // lockversion
            if (lockVersion.HasValue)
                payload["lockVersion"] = lockVersion.Value;

            var json = payload.ToString(Newtonsoft.Json.Formatting.None);

            // 1) update form validate
            using (var formReq = new HttpRequestMessage(HttpMethod.Post, $"api/v3/work_packages/{id}/form"))
            {
                formReq.Content = new StringContent(json, Encoding.UTF8, "application/json");
                _auth?.Apply(formReq);

                var formResp = await _http.SendAsync(formReq);
                var formBody = await formResp.Content.ReadAsStringAsync();
                if (!formResp.IsSuccessStatusCode)
                    throw new ApiException(formResp.StatusCode, formBody);
            }

            // 2) patch
            using var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"api/v3/work_packages/{id}");
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            var wp = JsonConvert.DeserializeObject<WorkPackage>(body) ?? new WorkPackage();
            wp.AddCustomFields(_customFieldRegistry);
            return wp;
        }

        public async Task DeleteWorkPackageAsync(int id)
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"api/v3/work_packages/{id}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);
        }

        public async Task<WorkPackage> UpdateWithRelationsAsync(int id, WorkPackageChangeSet cs, int? lockVersion = null)
        {
            var wp = await UpdateWorkPackageAsync(id, cs, lockVersion);

            // add relations
            foreach (var r in cs.AddRelations)
                await _rels.CreateRelationAsync(wp.Id, r.ToWorkPackageId, r.Type, r.Description, r.Lag);

            // delete relations
            foreach (var relId in cs.DeleteRelationIds)
                await _rels.DeleteRelationAsync(relId);

            return wp;
        }
    }

    public static class WorkPackagePayloadBuilder
    {
        public static JObject BuildPayload(WorkPackageChangeSet cs, CustomFieldRegistry reg)
        {
            var o = new JObject();

            if (cs.Subject != null) o["subject"] = cs.Subject;

            if (cs.DescriptionMarkdown != null)
            {
                o["description"] = new JObject
                {
                    ["format"] = "markdown",
                    ["raw"] = cs.DescriptionMarkdown
                };
            }

            if (cs.StartDate != null) o["startDate"] = cs.StartDate;
            if (cs.DueDate != null) o["dueDate"] = cs.DueDate;
            if (cs.LockVersion.HasValue) o["lockVersion"] = cs.LockVersion.Value;

            var links = new JObject();

            if (cs.ProjectId.HasValue) links["project"] = Href($"/api/v3/projects/{cs.ProjectId.Value}");
            if (cs.TypeId.HasValue) links["type"] = Href($"/api/v3/types/{cs.TypeId.Value}");
            if (cs.StatusId.HasValue) links["status"] = Href($"/api/v3/statuses/{cs.StatusId.Value}");
            if (cs.AssigneeId.HasValue) links["assignee"] = Href($"/api/v3/users/{cs.AssigneeId.Value}");

            // Custom field VALUES (properties)
            foreach (var kv in cs.CustomFieldValues)
            {
                var id = kv.Key;
                var def = reg.TryGet(id, out var d) ? d : null;

                var key = GetCustomFieldKey(id, def?.Type, isLinks: false);
                o[key] = kv.Value == null ? JValue.CreateNull() : JToken.FromObject(kv.Value);
            }

            // Custom field LINKS (for option lists, references, etc.)
            foreach (var kv in cs.CustomFieldLinkHrefs)
            {
                var id = kv.Key;
                var def = reg.TryGet(id, out var d) ? d : null;

                var rel = GetCustomFieldKey(id, def?.Type, isLinks: true);

                // For HAL links we need either object or array of objects. For multi, use array.
                var arr = new JArray();
                foreach (var href in kv.Value)
                    arr.Add(Href(href));

                // Even for single you *can* send array if schema expects array;
                // If your instance expects object for single, handle below:
                if (IsMulti(def?.Type))
                    links[rel] = arr;
                else
                    links[rel] = arr.Count > 0 ? arr[0] : null;
            }

            if (links.HasValues)
                o["_links"] = links;

            return o;
        }

        private static JObject Href(string href) => new JObject { ["href"] = href };

        private static bool IsMulti(string? openProjectType)
            => openProjectType != null && openProjectType.IndexOf("::Multi", StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// Decides whether to use customField{id} or customFields{id}.
        /// We prefer plural for Multi types.
        /// </summary>
        private static string GetCustomFieldKey(int id, string? openProjectType, bool isLinks)
        {
            // key/rel naming is same; location differs (property vs _links)
            var plural = IsMulti(openProjectType);
            return plural ? $"customFields{id}" : $"customField{id}";
        }
    }


}