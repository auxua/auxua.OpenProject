using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace auxua.OpenProject.Client
{
    public class ProjectsApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        public ProjectsApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

        public async Task<HalCollection<Project>> GetProjectsAsync(int pageSize = 10, int offset = 0)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "api/v3/projects?pageSize=" + pageSize + "&offset=" + offset);
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<Project>>(body)
                   ?? new HalCollection<Project>();
        }

        public Task<List<Project>> GetAllProjectsAsync(int pageSize = 10)
        {
            return PaginationHelper.FetchAllAsync<Project>(
                (page, ps) => GetProjectsAsync(ps, page),
                pageSize: pageSize,
                startPage: 1
            );
        }

        /*public async Task<IReadOnlyCollection<Project>> GetProjectsAllAsync(int chunksize=10)
        {
            HalCollection<Project>? allProjects = new HalCollection<Project>();
            allProjects.Embedded = new HalCollectionEmbedded<Project>();
            allProjects.Embedded.Elements = new List<Project>();
            // Get first set
            var resp = await GetProjectsAsync(chunksize,0);
            // Extract counts
            var total = resp.Total;
            var chunksNeeded = (int)Math.Ceiling((double)total / chunksize);
            allProjects.Total = resp.Total;
            allProjects.Embedded.Elements.AddRange(resp.Elements);

            for (int i=2; i<=chunksNeeded; i++)
            {
                Console.WriteLine("Call with Offset " + i);
                var offset = i;
                var nextResp = await GetProjectsAsync(chunksize, offset);
                allProjects.Embedded.Elements.AddRange(nextResp.Elements);
            }

            return allProjects.Elements;
        }*/
    }
}