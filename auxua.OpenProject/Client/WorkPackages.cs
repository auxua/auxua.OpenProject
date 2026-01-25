using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace auxua.OpenProject.Client
{
    public sealed class WorkPackagesApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;
        private readonly CustomFieldRegistry _customFieldRegistry;

        public WorkPackagesApi(HttpClient http, IAuthProvider? auth, CustomFieldRegistry customFieldRegistry)
        {
            _http = http;
            _auth = auth;
            _customFieldRegistry = customFieldRegistry;
        }

        /// <summary>
        /// Central method: fetches a page of work packages, optionally filtered.
        /// Also imports embedded schema custom field definitions into the instance registry.
        /// </summary>
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

        public Task<List<WorkPackage>> GetAllWorkPackagesAsync(int pageSize = 100)
            => GetAllWorkPackagesAsync(query: null, pageSize: pageSize);

        public Task<List<WorkPackage>> GetAllWorkPackagesAsync(WorkPackageQuery? query, int pageSize = 100)
        {
            return PaginationHelper.FetchAllAsync<WorkPackage>(
                (page, ps) => GetWorkPackagesAsync(query, ps, page),
                pageSize: pageSize,
                startPage: 1
            );
        }

        public Task<List<WorkPackage>> GetAllWorkPackagesForProjectAsync(int projectId, int pageSize = 100)
        {
            var q = WorkPackageQuery.ForProject(projectId);
            return GetAllWorkPackagesAsync(q, pageSize);
        }

        // ----------------------------
        // Query builder
        // ----------------------------
        public sealed class WorkPackageQuery
        {
            private readonly List<object> _filters = new();

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

            public string Build()
            {
                var json = JsonConvert.SerializeObject(_filters);
                return HttpUtility.UrlEncode(json);
            }
        }
    }
}