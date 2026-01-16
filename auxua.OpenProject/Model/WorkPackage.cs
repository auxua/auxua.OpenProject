using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace auxua.OpenProject.Model
{
    public sealed class WorkPackage : HalResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("subject")]
        public string? Subject { get; set; }

        [JsonProperty("description")]
        public WorkPackageDescription? Description { get; set; }

        [JsonProperty("lockVersion")]
        public int? LockVersion { get; set; }

        [JsonProperty("startDate")]
        public string? StartDate { get; set; } //TODO: Cast to DateTime?

        [JsonProperty("dueDate")]
        public string? DueDate { get; set; }//TODO: Cast to DateTime?

        [JsonProperty("createdAt")]
        public string? CreatedAt { get; set; }//TODO: Cast to DateTime?

        [JsonProperty("updatedAt")]
        public string? UpdatedAt { get; set; }//TODO: Cast to DateTime?

        [JsonExtensionData]
        public IDictionary<string, JToken>? Extra { get; set; }
    }

    public sealed class WorkPackageDescription
    {
        [JsonProperty("raw")]
        public string? Raw { get; set; }

        [JsonProperty("html")]
        public string? Html { get; set; }
    }


}
