using Newtonsoft.Json;
using System;

namespace auxua.OpenProject.Model
{
    public sealed class TimeEntry : HalResource
    {
        [JsonProperty("_type")] public string? Type { get; set; } // "TimeEntry"
        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("hours")] public string? Hours { get; set; } //Most likely: ISO8601 conform

        [JsonProperty("spentOn")] public DateTime? SpentOn { get; set; }

        [JsonProperty("comment")] public Formattable? Comment { get; set; }

        [JsonProperty("createdAt")] public DateTime? CreatedAt { get; set; }
        [JsonProperty("updatedAt")] public DateTime? UpdatedAt { get; set; }
    }
}
