using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace auxua.OpenProject.Model
{
    public sealed class HalCollection<T> : HalResource
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }




        [JsonProperty("_embedded")]
        public HalCollectionEmbedded<T>? Embedded { get; set; }

        // Komfort-Property: direkt typisierte Elemente
        [JsonIgnore]
        public IReadOnlyList<T> Elements => Embedded?.Elements ?? new List<T>();
    }

    public sealed class HalCollectionEmbedded<T>
    {
        [JsonProperty("elements")]
        public List<T>? Elements { get; set; }
    }
}
