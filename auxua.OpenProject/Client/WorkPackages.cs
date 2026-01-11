using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace auxua.OpenProject.Client
{
    public sealed class WorkPackagesApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        public WorkPackagesApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

        public async Task<HalCollection<WorkPackage>> GetWorkPackagesAsync(int pageSize = 20, int offset = 1)
        {
            // offset = page number (wie bei dir erprobt)
            using var req = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/v3/work_packages?pageSize={pageSize}&offset={offset}"
            );

            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<WorkPackage>>(body)
                   ?? new HalCollection<WorkPackage>();
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
        {
            return PaginationHelper.FetchAllAsync<WorkPackage>(
                (page, ps) => GetWorkPackagesAsync(ps, page),
                pageSize: pageSize,
                startPage: 1
            );
        }


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


        public async Task<HalCollection<WorkPackage>> GetWorkPackagesAsync(
                WorkPackageQuery query,
                int pageSize = 20,
                int page = 1)
        {
            var filterPart = query != null
                ? $"&filters={query.Build()}"
                : "";

            using var req = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/v3/work_packages?pageSize={pageSize}&offset={page}{filterPart}"
            );

            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<WorkPackage>>(body)
                   ?? new HalCollection<WorkPackage>();
        }

        public Task<List<WorkPackage>> GetAllWorkPackagesAsync(
            WorkPackageQuery? query,
            int pageSize = 100)
        {
            return PaginationHelper.FetchAllAsync<WorkPackage>(
                (page, ps) => GetWorkPackagesAsync(query, ps, page),
                pageSize: pageSize,
                startPage: 1
            );
        }

        public Task<List<WorkPackage>> GetAllWorkPackagesForProjectAsync(
            int projectId,
            int pageSize = 100)
        {
            var q = WorkPackageQuery.ForProject(projectId);
            return GetAllWorkPackagesAsync(q, pageSize);
        }
    }
}
