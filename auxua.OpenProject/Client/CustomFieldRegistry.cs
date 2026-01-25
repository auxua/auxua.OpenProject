using auxua.OpenProject.Model;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace auxua.OpenProject.Client
{
    public sealed class CustomFieldRegistry
    {
        private static readonly Regex CfKeyRegex =
            new(@"^customFields?(?<id>\d+)$", RegexOptions.Compiled);

        private readonly object _gate = new();
        private readonly Dictionary<int, CustomFieldDefinition> _byId = new();

        public IReadOnlyDictionary<int, CustomFieldDefinition> ById
        {
            get { lock (_gate) return new Dictionary<int, CustomFieldDefinition>(_byId); }
        }

        public bool TryGet(int id, out CustomFieldDefinition def)
        {
            lock (_gate)
                return _byId.TryGetValue(id, out def!);
        }

        /// <summary>
        /// Imports (merges) custom field definitions from a WorkPackages collection response
        /// that contains embedded schemas.
        /// </summary>
        public void ImportFromWorkPackagesCollectionJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            var root = JObject.Parse(json);

            // Path: _embedded -> schemas -> _embedded -> elements (array)
            var schemaElements = root.SelectToken("_embedded.schemas._embedded.elements") as JArray;
            if (schemaElements == null)
                return;

            foreach (var schemaEl in schemaElements)
            {
                if (schemaEl is not JObject schemaObj)
                    continue;

                ImportFromSingleSchemaObject(schemaObj);
            }
        }

        private void ImportFromSingleSchemaObject(JObject schemaObj)
        {
            foreach (var prop in schemaObj.Properties())
            {
                var m = CfKeyRegex.Match(prop.Name);
                if (!m.Success) continue;

                if (!int.TryParse(m.Groups["id"].Value, out var id))
                    continue;

                // Schema field definition is usually an object
                if (prop.Value is not JObject fieldObj)
                    continue;

                var def = new CustomFieldDefinition
                {
                    Id = id,
                    ApiKey = prop.Name,
                    Name = fieldObj["name"]?.Type == JTokenType.String ? fieldObj["name"]!.Value<string>() : null,
                    Type = fieldObj["type"]?.Type == JTokenType.String ? fieldObj["type"]!.Value<string>() : null,
                    Required = fieldObj["required"]?.Type == JTokenType.Boolean ? fieldObj["required"]!.Value<bool>() : (bool?)null,
                    Raw = fieldObj
                };

                lock (_gate)
                {
                    // Merge strategy:
                    // - If not present: add
                    // - If present: keep existing unless new has more info
                    if (!_byId.TryGetValue(id, out var existing))
                    {
                        _byId[id] = def;
                    }
                    else
                    {
                        _byId[id] = Merge(existing, def);
                    }
                }
            }
        }

        private static CustomFieldDefinition Merge(CustomFieldDefinition oldDef, CustomFieldDefinition newDef)
        {
            // prefer non-null values from newDef
            return new CustomFieldDefinition
            {
                Id = oldDef.Id,
                ApiKey = newDef.ApiKey ?? oldDef.ApiKey,
                Name = !string.IsNullOrWhiteSpace(newDef.Name) ? newDef.Name : oldDef.Name,
                Type = !string.IsNullOrWhiteSpace(newDef.Type) ? newDef.Type : oldDef.Type,
                Required = newDef.Required ?? oldDef.Required,
                Raw = newDef.Raw ?? oldDef.Raw
            };
        }
    }
}