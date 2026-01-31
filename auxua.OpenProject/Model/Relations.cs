using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace auxua.OpenProject.Model
{
    public sealed class Relation : HalResource
    {
        [JsonProperty("_type")] public string? ResourceType { get; set; } // "Relation"
        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("name")] public string? Name { get; set; }
        [JsonProperty("type")] public string? Type { get; set; } // "duplicates"...
        [JsonProperty("reverseType")] public string? ReverseType { get; set; }

        [JsonProperty("lag")] public int? Lag { get; set; } // Lag in days
        [JsonProperty("description")] public string? Description { get; set; }
    }

}
