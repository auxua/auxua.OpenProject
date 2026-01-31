using Newtonsoft.Json;
using System;

namespace auxua.OpenProject.Model
{
    public sealed class User : HalResource
    {
        [JsonProperty("_type")]
        public string? Type { get; set; } // "User"

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        // May be empty, due to privacy settings
        [JsonProperty("email")]
        public string? Email { get; set; }

        [JsonProperty("admin")]
        public bool? Admin { get; set; }

        [JsonProperty("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

    }
}
