using Newtonsoft.Json;

namespace auxua.OpenProject.Model
{
    public sealed class Formattable
    {
        [JsonProperty("format")] public string? Format { get; set; }  
        [JsonProperty("raw")] public string? Raw { get; set; }
        [JsonProperty("html")] public string? Html { get; set; }
    }

}
