using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class IntDictionaryConverter<TValue> : JsonConverter<Dictionary<int, TValue>>
    {
        public override Dictionary<int, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringDict = JsonSerializer.Deserialize<Dictionary<string, TValue>>(ref reader);
            return stringDict.ToDictionary(x => int.Parse(x.Key), x => x.Value);
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<int, TValue> value, JsonSerializerOptions options)
        {
            var stringDict = value.ToDictionary(x => x.Key.ToString(), x => x.Value);
            JsonSerializer.Serialize(writer, stringDict);
        }
    }
}
