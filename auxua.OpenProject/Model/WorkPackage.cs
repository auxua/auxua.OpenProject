using auxua.OpenProject.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace auxua.OpenProject.Model
{
    public sealed class WorkPackage : HalResource
    {
        private CustomFieldRegistry _customFieldRegistry = null!;

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("subject")]
        public string? Subject { get; set; }

        [JsonProperty("description")]
        public Formattable? Description { get; set; }

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

        public Dictionary<string, CustomFieldTyped> AdditionalFields { get; private set; }

        private WorkPackageFacade _wf = null!;

        public void AddCustomFields(CustomFieldRegistry reg)
        {
            this._customFieldRegistry = reg;
            this._wf = new WorkPackageFacade(this, reg);

            // Add Custom Field
            AdditionalFields = _wf.CustomFields;

            // Get the "standard" fields too
            foreach (var item in this.Extra)
            {
                if (item.Key.StartsWith("customField")) continue;
                AdditionalFields[item.Key] = new CustomFieldTyped(new CustomFieldValue() { });
            }
        }
    }

    //public sealed class WorkPackageDescription
    //{
    //    [JsonProperty("raw")]
    //    public string? Raw { get; set; }

    //    [JsonProperty("html")]
    //    public string? Html { get; set; }
    //}
}