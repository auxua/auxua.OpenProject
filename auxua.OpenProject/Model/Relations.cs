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

    public static class RelationType
    {
        public static readonly string Duplicates = "duplicates";
        public static readonly string Duplicated = "duplicated";
        public static readonly string Relates = "relates";
        public static readonly string Blocks = "blocks";
        public static readonly string Blocked = "blocked";
        public static readonly string Follows = "follows";
        public static readonly string Precedes = "precedes";
        public static readonly string Includes = "includes";
        public static readonly string Partof = "partof";
        public static readonly string Requires = "requires";
        public static readonly string Required = "requirede";
    }

}
