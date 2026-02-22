using Newtonsoft.Json;

namespace auxua.OpenProject.Model
{
    public class WorkPackageType : HalResource
    {
        //[JsonProperty("_type")] public string? Type { get; set; } // "Type"
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string? Name { get; set; }
        /// <summary>
        /// Color as Hex string
        /// </summary>
        [JsonProperty("color")] public string? Color { get; set; }
        [JsonProperty("isDefault")] public bool IsDefault { get; set; } = false;
        [JsonProperty("isMilestone")] public bool IsMilestone { get; set; } = false;

    }
    //public sealed class WorkPackageDescription
    //{
    //    [JsonProperty("raw")]
    //    public string? Raw { get; set; }

    //    [JsonProperty("html")]
    //    public string? Html { get; set; }
    //}


}