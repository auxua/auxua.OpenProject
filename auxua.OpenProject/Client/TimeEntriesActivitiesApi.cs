using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace auxua.OpenProject.Client
{
    public sealed class TimeEntryActivitiesApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        public TimeEntryActivitiesApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

        public async Task<HalCollection<TimeEntriesActivity>> GetActivitiesAsync(int pageSize = 100, int page = 1)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/time_entry_activities?pageSize={pageSize}&offset={page}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<HalCollection<TimeEntriesActivity>>(body)
                   ?? new HalCollection<TimeEntriesActivity>();
        }
    }
}
