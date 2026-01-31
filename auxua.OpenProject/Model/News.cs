using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace auxua.OpenProject.Model
{
    using Newtonsoft.Json;
    using System;

    namespace auxua.OpenProject.Model
    {
        public sealed class News : HalResource
        {
            [JsonProperty("_type")] public string? Type { get; set; } // "News"
            [JsonProperty("id")] public int Id { get; set; }

            [JsonProperty("title")] public string? Title { get; set; }
            [JsonProperty("summary")] public string? Summary { get; set; }

            [JsonProperty("description")] public Formattable? Description { get; set; }

            [JsonProperty("createdAt")] public DateTime? CreatedAt { get; set; }
            [JsonProperty("updatedAt")] public DateTime? UpdatedAt { get; set; } // optional

            public string? Project { get; private set; }

            public void PostProcess()
            {
                var proj = this.Links["project"]?.First();
                var title = proj.Next.Last.ToString();

                this.Project = title;
                //this.Project = this.Links["project"]?.First()["title"].ToString();
            }
        }
    }

}
