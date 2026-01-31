using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace auxua.OpenProject.Model
{
    public sealed class Activity : HalResource
    {
        [JsonProperty("_type")] public string? Type { get; set; }  // "Activity" or "Activity::Comment"
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("version")] public int Version { get; set; }

        [JsonProperty("comment")] public Formattable? Comment { get; set; }
        [JsonProperty("details")] public List<Formattable>? Details { get; set; }

        [JsonProperty("createdAt")] public DateTime? CreatedAt { get; set; }
        [JsonProperty("updatedAt")] public DateTime? UpdatedAt { get; set; }

        [JsonProperty("internal")] public bool? Internal { get; set; } // in some examples?
    }

}
