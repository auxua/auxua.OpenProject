using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace auxua.OpenProject.Model
{
    public sealed class Project : HalResource
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string? Name { get; set; }
        [JsonProperty("active")] public bool? Active { get; set; }
        [JsonProperty("public")] public bool? Public { get; set; }
        [JsonProperty("createdAt")] public DateTime? CreatedAt { get; set; }
        [JsonProperty("updatedAt")] public DateTime? UpdatedAt { get; set; }
        [JsonProperty("description")] public TextDescription? Description { get; set; }
        [JsonProperty("statusExplanation")] public TextDescription? StatusExplanation { get; set; }
    }

    public class TextDescription
    {
        [JsonProperty("format")] public string? Format { get; set; }
        [JsonProperty("raw")] public string? Raw { get; set; }
        [JsonProperty("html")] public string? Html { get; set; }
    }
}
