using Newtonsoft.Json;

namespace auxua.OpenProject.Model
{
    public sealed class TimeEntriesActivity : HalResource
    {
        [JsonProperty("_type")] public string? Type { get; set; }
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string? Name { get; set; }
        [JsonProperty("default")] public bool? Default { get; set; }
        [JsonProperty("position")] public int? Position { get; set; }
    }
}
