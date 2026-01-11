using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace auxua.OpenProject.Model
{
    public sealed class WorkPackage : HalResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("subject")]
        public string? Subject { get; set; }

        // "description" ist ein Objekt (raw/html), nicht nur string
        [JsonProperty("description")]
        public WorkPackageDescription? Description { get; set; }

        [JsonProperty("lockVersion")]
        public int? LockVersion { get; set; }

        [JsonProperty("startDate")]
        public string? StartDate { get; set; } // später DateOnly/DateTime

        [JsonProperty("dueDate")]
        public string? DueDate { get; set; }

        [JsonProperty("createdAt")]
        public string? CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public string? UpdatedAt { get; set; }
    }

    public sealed class WorkPackageDescription
    {
        [JsonProperty("raw")]
        public string? Raw { get; set; }

        [JsonProperty("html")]
        public string? Html { get; set; }
    }
}
