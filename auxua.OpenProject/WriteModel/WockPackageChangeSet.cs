using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace auxua.OpenProject.WriteModel
{
    public class WorkPackageChangeSet
    {
        // Main Fields
        public string? Subject { get; set; }
        public string? DescriptionMarkdown { get; set; }
        public string? StartDate { get; set; }   // "yyyy-MM-dd"
        public string? DueDate { get; set; }
        public int? LockVersion { get; set; }

        // Links (IDs)
        public int? ProjectId { get; set; }
        public int? TypeId { get; set; }
        public int? StatusId { get; set; }
        public int? AssigneeId { get; set; }

        // Custom Fields (nach ID)
        // scalar/object/array 
        public Dictionary<int, object?> CustomFieldValues { get; } = new();

        // CustomField-Link-Values (für List/Optionen etc.)
        // z.B. customField8 -> list of hrefs
        public Dictionary<int, List<string>> CustomFieldLinkHrefs { get; } = new();

        // Relations 
        public List<RelationCreateSpec> AddRelations { get; } = new();
        public List<int> DeleteRelationIds { get; } = new();


        internal static JObject BuildWorkPackagePayload(WorkPackageChangeSet cs)
        {
            var o = new JObject();

            if (cs.Subject != null) o["subject"] = cs.Subject;

            if (cs.DescriptionMarkdown != null)
            {
                o["description"] = new JObject
                {
                    ["format"] = "markdown",
                    ["raw"] = cs.DescriptionMarkdown
                };
            }

            if (cs.StartDate != null) o["startDate"] = cs.StartDate;
            if (cs.DueDate != null) o["dueDate"] = cs.DueDate;

            // CustomField scalar values as properties:
            foreach (var kv in cs.CustomFieldValues)
            {
                // singular key; bei multi kann es auch customFields{ID} sein – aber
                // du solltest das aus deinem Schema/Registry wissen.
                // Für MVP: zuerst customField{ID} setzen; falls du in der Registry weißt dass Multi: customFields{ID}.
                var key = $"customField{kv.Key}";
                o[key] = kv.Value == null ? JValue.CreateNull() : JToken.FromObject(kv.Value);
            }

            // Links:
            var links = new JObject();

            if (cs.ProjectId.HasValue) links["project"] = new JObject { ["href"] = $"/api/v3/projects/{cs.ProjectId.Value}" };
            if (cs.TypeId.HasValue) links["type"] = new JObject { ["href"] = $"/api/v3/types/{cs.TypeId.Value}" };
            if (cs.StatusId.HasValue) links["status"] = new JObject { ["href"] = $"/api/v3/statuses/{cs.StatusId.Value}" };
            if (cs.AssigneeId.HasValue) links["assignee"] = new JObject { ["href"] = $"/api/v3/users/{cs.AssigneeId.Value}" };

            // CustomField link values (list/reference custom fields)
            foreach (var kv in cs.CustomFieldLinkHrefs)
            {
                // rel name: customField{ID} oder customFields{ID} – du kennst es aus Schema/Registry
                var rel = $"customField{kv.Key}";

                var arr = new JArray();
                foreach (var href in kv.Value)
                    arr.Add(new JObject { ["href"] = href });

                // Im HAL-Body müssen arrays als array kommen
                links[rel] = arr;
            }

            if (links.HasValues) o["_links"] = links;

            return o;
        }

}

public class RelationCreateSpec
    {
        public int ToWorkPackageId { get; set; }
        public string Type { get; set; } // duplicates, relates, follows, ...
        public int? Lag { get; set; }
        public string? Description { get; set; }

        public RelationCreateSpec(int toWorkPackageId, string type, int? lag=0, string description="")
        {
            ToWorkPackageId = toWorkPackageId;
            Type = type;
            Lag = lag;
            Description = description;
        }


    }

}
