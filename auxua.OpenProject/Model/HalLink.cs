using Newtonsoft.Json;

namespace auxua.OpenProject.Model
{
    public sealed class HalLink
    {
        [JsonProperty("href")] public string? Href { get; set; }
        [JsonProperty("title")] public string? Title { get; set; }
        [JsonProperty("method")] public string? Method { get; set; }
    }
}