using Newtonsoft.Json;
using System;

namespace auxua.OpenProject.Model
{
    public sealed class Version : HalResource
    {
        [JsonProperty("_type")] public string? Type { get; set; } // "Version"
        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("name")] public string? Name { get; set; }
        [JsonProperty("description")] public Formattable? Description { get; set; }

        [JsonProperty("startDate")] public DateTime? StartDate { get; set; }
        [JsonProperty("endDate")] public DateTime? EndDate { get; set; }

        [JsonProperty("status")] public string? Status { get; set; } // "open", "closed" 
        [JsonProperty("sharing")] public string? Sharing { get; set; } // "none", "system", ... 

        [JsonProperty("createdAt")] public DateTime? CreatedAt { get; set; }
        [JsonProperty("updatedAt")] public DateTime? UpdatedAt { get; set; }
    }
}
