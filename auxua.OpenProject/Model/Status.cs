using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace auxua.OpenProject.Model
{
    public class Status : HalResource
    {
        //[JsonProperty("_type")] public string? Type { get; set; } // "Type"
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string? Name { get; set; }
        /// <summary>
        /// Color as Hex string
        /// </summary>
        [JsonProperty("color")] public string? Color { get; set; }
        [JsonProperty("isClosed")] public bool IsClosed { get; set; } = false;
        [JsonProperty("isDefault")] public bool IsDefault { get; set; } = false;
        [JsonProperty("isReadonly")] public bool IsReadonly { get; set; } = false;
        [JsonProperty("defaultDoneRatio")] public int? DefaultDoneRatio { get; set; } // 0<=x<=100
        [JsonProperty("excludedFromTotals")] public bool ExcludedFromTotals { get; set; } = false;

    }

    public class StatusCollection : HalCollection<Status>
    {
        //[JsonProperty("_embedded")]
        //public new NotificationCollectionEmbedded? Embedded { get; set; }


        //[JsonIgnore]
        //public List<Notification> Elements => Embedded?.Elements ?? new List<Notification>();
    }

    
}
