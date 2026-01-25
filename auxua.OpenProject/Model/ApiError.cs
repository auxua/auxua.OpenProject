using Newtonsoft.Json;

namespace auxua.OpenProject.Model
{
    public sealed class ApiError
    {
        [JsonProperty("_type")] public string? Type { get; set; }
        [JsonProperty("errorIdentifier")] public string? ErrorIdentifier { get; set; }
        [JsonProperty("message")] public string? Message { get; set; }
    }
}