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
        [JsonProperty("identifier")] public string? Identifier { get; set; }
    }
}
