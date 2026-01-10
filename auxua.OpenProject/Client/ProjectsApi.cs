using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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

        public async Task<HalCollection<Project>> GetProjectsAsync()
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "api/v3/projects");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<Project>>(body)
                   ?? new HalCollection<Project>();
        }
    }
}
