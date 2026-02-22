using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace auxua.OpenProject.REST
{
    internal static class RequestHelper
    {
        internal async static Task PostNoContentAsync(string url, HttpClient _http, IAuthProvider? _auth, bool exceptionOnError = true)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url);

            // Sometimes, OP reacts with errors to empty/missing POST Actions -> Using empty json will do
            req.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode && exceptionOnError) throw new ApiException(resp.StatusCode, body);
        }

        internal async static Task<string> PostStringAsync(string url, string content, HttpClient _http, IAuthProvider? _auth, bool exceptionOnError = true)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url);

            // Sometimes, OP reacts with errors to empty/missing POST Actions -> Using empty json will do
            req.Content = new StringContent(content, Encoding.UTF8, "application/json");

            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode && exceptionOnError) throw new ApiException(resp.StatusCode, body);
            return body;
        }

        internal async static Task<string> GetAsStringAsync(string url, HttpClient _http, IAuthProvider? _auth, bool exceptionOnError=true)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            _auth?.Apply(req);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode && exceptionOnError) throw new ApiException(resp.StatusCode, body);
            return body;
        }
    }
}
