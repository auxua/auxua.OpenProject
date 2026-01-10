using System;
using System.Collections.Generic;
using System.Text;

namespace auxua.OpenProject.Model
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public sealed class SingleOrArrayConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(List<T>);

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            if (token.Type == JTokenType.Array)
                return token.ToObject<List<T>>(serializer) ?? new List<T>();

            if (token.Type == JTokenType.Null)
                return new List<T>();

            // single object -> wrap into list
            var single = token.ToObject<T>(serializer);
            return single != null ? new List<T> { single } : new List<T>();
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
