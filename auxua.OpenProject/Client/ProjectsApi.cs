using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace auxua.OpenProject.Client
{
    /// <summary>
    /// Client for interacting with OpenProject projects
    /// Provides methods to list projects and fetch all projects using pagination helpers
    /// </summary>
    public class ProjectsApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        /// <summary>
        /// Create a new <see cref="ProjectsApi"/> instance
        /// </summary>
        /// <param name="http">The <see cref="HttpClient"/> used to send requests to the OpenProject API.</param>
        /// <param name="auth">Optional authentication provider that will apply authentication headers to requests.</param>
        public ProjectsApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

        /// <summary>
        /// Retrieve a page of projects from the OpenProject API
        /// </summary>
        /// <param name="pageSize">The number of projects per page. Defaults to 10.</param>
        /// <param name="offset">The page offset to retrieve. Defaults to 0.</param>
        /// <returns>A task that resolves to a <see cref="HalCollection{Project}"/> containing the projects for the requested page.</returns>
        /// <exception cref="ApiException">Thrown when the API returns a non-success status code. The exception contains the HTTP status code and response body.</exception>
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

        /// <summary>
        /// Fetch all projects by iterating the paginated endpoint using the pagination helper
        /// </summary>
        /// <param name="pageSize">Number of projects to request per page when fetching. Defaults to 10.</param>
        /// <returns>A task that resolves to a list containing all projects.</returns>
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