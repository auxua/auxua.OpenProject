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
    public class StatusApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;
        private readonly StatusRegistry _reg;

        public StatusApi(HttpClient http, IAuthProvider? auth, StatusRegistry reg)
        {
            _http = http;
            _auth = auth;
            _reg = reg;
        }

        /// <summary>
        /// Retrieve a page of statuses from the OpenProject API.
        /// </summary>
        public async Task<StatusCollection> GetStatusesAsync(
            int pageSize = 100,
            int offset = 1)
        {
            var url = $"api/v3/statuses?pagesize={pageSize}&offset={offset}";

            var body = await REST.RequestHelper.GetAsStringAsync(url, _http, _auth);

            var res = JsonConvert.DeserializeObject<StatusCollection>(body)
                   ?? new StatusCollection();

            _reg.UpsertMany(res.Embedded.Elements);

            return res;
        }

        /// <summary>
        /// Fetch all Statuses by iterating the paginated endpoint.
        /// </summary>
        public Task<List<Status>> GetAllStatusesAsync(
            int pageSize = 100)
        {
            return PaginationHelper.FetchAllAsync<Status>(
                async (page, ps) =>
                {
                    //Console.WriteLine("Called!");
                    var collection = await GetStatusesAsync(ps, page);

                    return collection;
                },
                pageSize: pageSize,
                startPage: 1
            );
        }
    }


    public class StatusRegistry
    {
        private readonly object _gate = new();
        private readonly Dictionary<int, Status> _byId = new();
        private readonly Dictionary<string, int> _byName = new(StringComparer.OrdinalIgnoreCase);

        public void UpsertMany(IEnumerable<Status> types)
        {
            foreach (var t in types) Upsert(t);
        }

        public void Upsert(Status t)
        {
            lock (_gate)
            {
                _byId[t.Id] = t;
                if (!string.IsNullOrWhiteSpace(t.Name))
                    _byName[t.Name!] = t.Id;
            }
        }

        public bool TryGetIdByName(string name, out int id)
        {
            lock (_gate) return _byName.TryGetValue(name, out id);
        }

        public bool TryGetById(int id, out Status t)
        {
            lock (_gate) return _byId.TryGetValue(id, out t!);
        }
    }
    
}
