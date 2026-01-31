using auxua.OpenProject.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace auxua.OpenProject.Model
{
    public sealed class WorkPackageFacade
    {
        private static readonly Regex CfKey =
            new(@"^customFields?(?<id>\d+)$", RegexOptions.Compiled);

        private readonly WorkPackage _wp;
        private readonly CustomFieldRegistry _registry;

        public Dictionary<string, object> FlattenedFields { get; private set; } = new Dictionary<string, object>();

        public int Id => _wp.Id;
        public string Subject => _wp.Subject;
        public Formattable? Description => _wp.Description;
        public string? Type => _wp.Type;

        public string? Status { get; private set; }
        public string? OpType { get; private set; }

        public string? Parent { get; private set; }

        public Dictionary<string, CustomFieldTyped> CustomFields { get; private set; } = new Dictionary<string, CustomFieldTyped>();

        public WorkPackageFacade(WorkPackage wp, CustomFieldRegistry registry)
        {
            _wp = wp;
            _registry = registry;

            ExtractValues();

            Flatten();

            GetRelations();
        }

        private void GetRelations()
        {
            
        }

        /// <summary>
        /// Flattens all fields (main fields, Extra, custom fields) into a single dictionary for easy access.
        /// </summary>
        private void Flatten()
        {
            // First, the Main Fields
            this.FlattenedFields["Status"] = this.Status;
            this.FlattenedFields["OpType"] = this.OpType;
            this.FlattenedFields["Type"] = this.Type;
            this.FlattenedFields["Parent"] = this.Parent;
            this.FlattenedFields["Subject"] = this.Subject;
            this.FlattenedFields["Description"] = this.Description != null ? this.Description.Raw : null;

            // Then, the Extra Fields
            foreach (var item in _wp.Extra)
            {
                if (item.Key.StartsWith("customField")) continue; // Skip custom fields here
                if (this.FlattenedFields.ContainsKey(item.Key))
                {
                    Console.WriteLine($"[WorkPackageFacade] Warning: Field '{item.Key}' from Extra conflicts with main field. Skipping.");
                    continue; // Skip if already exists (main fields take precedence)
                }
                if (item.Value is Newtonsoft.Json.Linq.JValue value)
                {
                    this.FlattenedFields[item.Key] = value.Value;
                }
                else if (item.Value is Newtonsoft.Json.Linq.JObject obj)
                {
                    this.FlattenedFields[item.Key] = obj;
                }
                //var jval = value;
                //this.FlattenedFields[item.Key] = jval.Value;
            }

            // Custon Fields
            foreach (var kv in this.CustomFields)
            {
                if (this.FlattenedFields.ContainsKey(kv.Key))
                {
                    Console.WriteLine($"[WorkPackageFacade] Warning: Custom Field '{kv.Key}' conflicts with existing field. Skipping.");
                    continue; // Skip if already exists
                }
                this.FlattenedFields[kv.Key] = kv.Value.Value;
            }

            // Additionals
            this.FlattenedFields["Id"] = this.Id;
            this.FlattenedFields["CreatedAt"] = this._wp.CreatedAt;
            this.FlattenedFields["UpdatedAt"] = this._wp.UpdatedAt;
            this.FlattenedFields["DueDate"] = this._wp.DueDate;
            this.FlattenedFields["LockVersion"] = this._wp.LockVersion;

        }

        private void ExtractValues()
        {
            var l = this._wp.Links.Where(x => x.Key.StartsWith("type")).First();
            OpType = l.Value["title"].ToString();

            l = this._wp.Links.Where(x => x.Key.StartsWith("status")).First();
            Status = l.Value["title"].ToString();

            l = this._wp.Links.Where(x => x.Key.StartsWith("parent")).First();
            Parent = l.Value["title"].ToString();

            // TODO: Further Fields as needed
            var cf = GetCustomFields();
            foreach (var item in cf)
            {
                int i = 0;
                if (item.Value.Name == null) continue;
                this.CustomFields.Add(item.Value.Name, new CustomFieldTyped(item.Value));
            }
        }

        public Dictionary<int, CustomFieldValue> GetCustomFields()
        {
            var result = new Dictionary<int, CustomFieldValue>();

            // A) Scalar / direct values from Extra: customField12, customFields8, etc.
            if (_wp.Extra != null)
            {
                foreach (var kv in _wp.Extra)
                {
                    var m = CfKey.Match(kv.Key);
                    if (!m.Success) continue;
                    if (!int.TryParse(m.Groups["id"].Value, out var id)) continue;

                    if (!result.TryGetValue(id, out var cf))
                    {
                        cf = new CustomFieldValue { Id = id };
                        result[id] = cf;
                    }

                    cf.Value = kv.Value;
                }
            }

            // B) List/Reference values from _links: _links.customField8 or _links.customFields8
            // We iterate all link rels and pick those matching customField(s){ID}
            if (_wp.Links != null)
            {
                foreach (var rel in _wp.Links.Keys)
                {
                    var m = CfKey.Match(rel);
                    if (!m.Success) continue;
                    if (!int.TryParse(m.Groups["id"].Value, out var id)) continue;

                    var links = _wp.GetLinks(rel); // should return IEnumerable<HalLink>
                    if (links == null) continue;

                    if (!result.TryGetValue(id, out var cf))
                    {
                        cf = new CustomFieldValue { Id = id };
                        result[id] = cf;
                    }

                    cf.Links.AddRange(links);
                }
            }

            // C) Add display names from registry (instanzweit)
            foreach (var kv in result)
            {
                if (_registry.TryGet(kv.Key, out var def))
                {
                    kv.Value.Name = def.Name;
                }
            }

            return result;
        }
    }
}