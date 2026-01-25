using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace auxua.OpenProject.Model
{
    public abstract class HalResource
    {
        [JsonProperty("_type")]
        public string? Type { get; set; }

        [JsonProperty("_links")]
        public Dictionary<string, JToken>? Links { get; set; }

        public IReadOnlyList<HalLink> GetLinks(string rel)
        {
            if (Links == null || !Links.TryGetValue(rel, out var token) || token == null)
                return Array.Empty<HalLink>();

            if (token.Type == JTokenType.Array)
            {
                var list = token.ToObject<List<HalLink>>();
                return list != null ? list : Array.Empty<HalLink>();
            }

            // single object
            var single = token.ToObject<HalLink>();
            return single != null ? new[] { single } : Array.Empty<HalLink>();
        }

        public HalLink? GetLink(string rel) => GetLinks(rel).FirstOrDefault();
    }
}