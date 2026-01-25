using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace auxua.OpenProject.Model
{
    public class CustomFieldDefinition
    {
        public int Id { get; set; }

        public string ApiKey { get; set; }

        public string? Name { get; set; }
        public string? Type { get; set; }
        public bool? Required { get; set; }

        public JObject? Raw { get; set; }

        public enum CustomFieldKind
        {
            String,
            Text,
            Integer,
            Float,
            Boolean,
            Date,
            OptionSingle,
            OptionMulti,
            Reference
        }

        public static CustomFieldKind FromOpenProjectType(string? type)
        {
            return type switch
            {
                "String" => CustomFieldKind.String,
                "Text" => CustomFieldKind.Text,
                "Integer" => CustomFieldKind.Integer,
                "Float" => CustomFieldKind.Float,
                "Boolean" => CustomFieldKind.Boolean,
                "Date" => CustomFieldKind.Date,
                "CustomOption" => CustomFieldKind.OptionSingle,
                "CustomOption::Multi" => CustomFieldKind.OptionMulti,
                _ => CustomFieldKind.Reference
            };
        }
    }

    public static class CustomFieldExtractor
    {
        private static readonly Regex CfKey =
            new(@"^customFields?(?<id>\d+)$", RegexOptions.Compiled);

        public static Dictionary<int, JToken> ExtractAndMergeById(WorkPackage wp)
        {
            var result = new Dictionary<int, JToken>();

            if (wp.Extra == null)
                return result;

            foreach (var kv in wp.Extra)
            {
                var m = CfKey.Match(kv.Key);
                if (!m.Success) continue;
                if (!int.TryParse(m.Groups["id"].Value, out var id)) continue;

                var token = kv.Value;

                if (!result.TryGetValue(id, out var existing))
                {
                    result[id] = token;
                    continue;
                }

                // - Array + Array => concat
                // - Array + scalar => append scalar
                // - scalar + Array => prepend scalar
                // - scalar + scalar => keep existing
                result[id] = Merge(existing, token);
            }

            return result;
        }

        private static JToken Merge(JToken a, JToken b)
        {
            if (a.Type == JTokenType.Array && b.Type == JTokenType.Array)
            {
                var merged = new JArray();
                foreach (var x in (JArray)a) merged.Add(x);
                foreach (var x in (JArray)b) merged.Add(x);
                return merged;
            }

            if (a.Type == JTokenType.Array && b.Type != JTokenType.Array)
            {
                var merged = new JArray();
                foreach (var x in (JArray)a) merged.Add(x);
                merged.Add(b);
                return merged;
            }

            if (a.Type != JTokenType.Array && b.Type == JTokenType.Array)
            {
                var merged = new JArray();
                merged.Add(a);
                foreach (var x in (JArray)b) merged.Add(x);
                return merged;
            }

            return a;
        }
    }

    public class CustomFieldTyped
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public object? Value { get; set; }

        public CustomFieldTyped(CustomFieldValue val)
        {
            this.Id = val.Id;
            this.Name = val.Name;

            // Case: Scalar value
            if (val.Value != null)
            {
                switch (val.Value.Type)
                {
                    case JTokenType.Integer:
                        this.Value = val.Value.ToObject<int>();
                        break;

                    case JTokenType.Float:
                        this.Value = val.Value.ToObject<double>();
                        break;

                    case JTokenType.Boolean:
                        this.Value = val.Value.ToObject<bool>();
                        break;

                    case JTokenType.String:
                        this.Value = val.Value.ToObject<string>();
                        break;

                    case JTokenType.Date:
                        this.Value = val.Value.ToObject<DateTime>();
                        break;

                    default:
                        this.Value = val.Value.ToString();
                        break;
                }
                return;
            }

            // Case: List or special
            var values = new List<string>();
            foreach (var link in val.Links)
            {
                if (!string.IsNullOrWhiteSpace(link.Title))
                    values.Add(link.Title!);
                else if (!string.IsNullOrWhiteSpace(link.Href))
                    values.Add(link.Href!);
            }
            this.Value = values;
        }
    }

    public sealed class CustomFieldValue
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public JToken? Value { get; set; }

        public List<HalLink> Links { get; set; } = new List<HalLink>();

        public CustomFieldValue()
        {
            
        }

        public CustomFieldValue(int id, string? name, JToken? value, List<HalLink> links)
        {
            Id = id;
            Name = name;
            Value = value;
            Links = links;
        }

        public static List<string> GetLinkTitles(CustomFieldValue cf)
        {
            var titles = new List<string>();
            foreach (var l in cf.Links)
            {
                if (!string.IsNullOrWhiteSpace(l.Title))
                    titles.Add(l.Title!);
            }
            return titles;
        }
    }
}