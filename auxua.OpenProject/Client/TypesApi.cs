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
    public sealed class TypesApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;
        private readonly WorkPackageTypeRegistry _registry;

        public TypesApi(HttpClient http, IAuthProvider? auth, WorkPackageTypeRegistry registry)
        {
            _http = http;
            _auth = auth;
            _registry = registry;
        }

        public async Task<HalCollection<WorkPackageType>> GetTypesAsync(int pageSize = 100, int page = 1)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/types?pageSize={pageSize}&offset={page}");
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new ApiException(resp.StatusCode, body);

            var col = JsonConvert.DeserializeObject<HalCollection<WorkPackageType>>(body)
                      ?? new HalCollection<WorkPackageType>();

            // fill registry
            _registry.UpsertMany(col.Elements);

            return col;
        }

        public Task<List<WorkPackageType>> GetAllTypesAsync(int pageSize = 100)
        {
            return PaginationHelper.FetchAllAsync<WorkPackageType>(
                async (page, ps) =>
                {
                    var c = await GetTypesAsync(ps, page);
                    return c;
                },
                pageSize: pageSize,
                startPage: 1
            );
        }
    }

}
