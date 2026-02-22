using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace auxua.OpenProject.Model
{
    /// <summary>
    /// OpenProject in-app notification
    /// </summary>
    public class Notification : HalResource
    {
        [JsonProperty("id")] public int Id { get; set; }

        // Old Documentation shows Subject?
        //[JsonProperty("subject")] public string? Subject { get; set; }

        [JsonProperty("reason")] public string? Reason { get; set; }
        [JsonProperty("readIAN")] public bool? ReadIAN { get; set; }

        [JsonProperty("createdAt")] public DateTime? CreatedAt { get; set; }
        [JsonProperty("updatedAt")] public DateTime? UpdatedAt { get; set; }

        [JsonProperty("_embedded")] public NotificationEmbedded? Embedded { get; set; }
        //public NotificationEmbedded? EmbeddedNotification => this.Embedded as NotificationEmbedded;

        public override string ToString()
        {
            if (this.Embedded!=null && !this.Embedded.IsEmpty)
                return $"{Embedded.Project?.Name} {Embedded.ActorOrAuthor?.Name} " +
                    $"{Embedded.Activity.Details.First()?.Raw} " +
                    $"on {Embedded.Resource.Subject} at {CreatedAt}";
            else
                return $"Notification {Id} (Reason: {Reason}, Read: {ReadIAN}, CreatedAt: {CreatedAt})";
        }
    }

    // In Documentation - in test instance, never was filled with data?
    public class NotificationEmbedded
    {
        // In Docu, sometimes actor, sometimes author is used - we use both for safety
        [JsonProperty("actor")] public User? Actor { get; set; }
        [JsonProperty("author")] public User? Author { get; set; }

        [JsonProperty("project")] public Project? Project { get; set; }
        [JsonProperty("resource")] public WorkPackage? Resource { get; set; }
        [JsonProperty("activity")] public Activity? Activity { get; set; }

        [JsonProperty("details")] public List<NotificationDetail>? Details { get; set; }

        [JsonIgnore]
        public User? ActorOrAuthor => Actor ?? Author;

        public bool IsEmpty =>
            ActorOrAuthor == null &&
            Project == null &&
            Resource == null &&
            Activity == null &&
            (Details == null || Details.Count == 0);
    }

    /// <summary>
    /// Detail element of a notification (Values::Property)
    /// </summary>
    public class NotificationDetail : HalResource
    {
        [JsonProperty("_type")] public string? Type { get; set; } // typically "Values::Property"
        [JsonProperty("property")] public string? Property { get; set; }
        [JsonProperty("value")] public string? Value { get; set; }
    }

    /// <summary>
    /// Notification collection response embeds "detailsSchemas" in addition to the usual "elements".
    /// </summary>
    public class NotificationCollection : HalCollection<Notification>
    {
        //[JsonProperty("_embedded")]
        //public new NotificationCollectionEmbedded? Embedded { get; set; }


        [JsonIgnore]
        public List<Notification> Elements => Embedded?.Elements ?? new List<Notification>();
    }

    public class NotificationCollectionEmbedded
    {
        [JsonProperty("elements")] public List<Notification>? Elements { get; set; }

        // Optimierung für Details-Rendering (z.B. dateAlert startDate/dueDate etc.)
        [JsonProperty("detailsSchemas")] public List<NotificationDetailSchema>? DetailsSchemas { get; set; }
    }

    /// <summary>
    /// Schema objects embedded as "detailsSchemas" for notification details
    /// </summary>
    public class NotificationDetailSchema : HalResource
    {
        [JsonProperty("property")] public NotificationSchemaField? Property { get; set; }
        [JsonProperty("value")] public NotificationSchemaField? Value { get; set; }
    }

    public class NotificationSchemaField
    {
        [JsonProperty("name")] public string? Name { get; set; }
        [JsonProperty("type")] public string? Type { get; set; }
    }

    public static class NotificationFilters
    {
        /// <summary>Only unread notifications</summary>
        public static string Unread() =>
            "[{ \"readIAN\": { \"operator\": \"=\", \"values\": [\"f\"] } }]";

        /// <summary>Only read notifications</summary>
        public static string Read() =>
            "[{ \"readIAN\": { \"operator\": \"=\", \"values\": [\"t\"] } }]";

        public static string Reason(string reason) =>
            $"[{{ \"reason\": {{ \"operator\": \"=\", \"values\": [\"{Escape(reason)}\"] }} }}]";

        public static string ProjectId(int projectId) =>
            $"[{{ \"project\": {{ \"operator\": \"=\", \"values\": [\"{projectId}\"] }} }}]";

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}