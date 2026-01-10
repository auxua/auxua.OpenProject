using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace auxua.OpenProject.REST
{
    public static class RESTClient
    {
        private static readonly HttpClient _client = CreateClient();

        private static HttpClient CreateClient()
        {
            var c = new HttpClient();
            c.DefaultRequestHeaders.Accept.Clear();
            c.DefaultRequestHeaders.Accept.ParseAdd("application/hal+json");
            c.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            return c;
        }

        public static async Task<string> GetAsync(string url)
        {
            var resp = await _client.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            resp.EnsureSuccessStatusCode();
            return body;
        }

        public static async Task<string> PostAsync(string url, string data)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(data, Encoding.UTF8, "application/json")
            };

            var resp = await _client.SendAsync(request);
            var body = await resp.Content.ReadAsStringAsync();
            resp.EnsureSuccessStatusCode();
            return body;
        }
    }

}
