using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class CortexModerationFlagConverter : JsonConverter<ModerationFlag>
    {
        public override ModerationFlag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Parse(reader.GetString());

        public override void Write(Utf8JsonWriter writer, ModerationFlag value, JsonSerializerOptions options) =>
            writer.WriteStringValue(ToJsonString(value));
        
        private static string ToJsonString(ModerationFlag self)
        {
            switch (self)
            {
                case ModerationFlag.OnTopic: return "ontopic";
                case ModerationFlag.Nws: return "nws";
                case ModerationFlag.Stupid: return "stupid";
                case ModerationFlag.Political: return "political";
                case ModerationFlag.Tangent: return "tangent";
                case ModerationFlag.Informative: return "informative";
                case ModerationFlag.Nuked: return "nuked";
                case ModerationFlag.Cortex: return "cortex";
                default: throw new ArgumentOutOfRangeException(nameof(self));
            }
        }

        public static ModerationFlag Parse(string str)
        {
            switch (str)
            {
                case "ontopic": return ModerationFlag.OnTopic;
                case "nws": return ModerationFlag.Nws;
                case "stupid": return ModerationFlag.Stupid;
                case "political": return ModerationFlag.Political;
                case "offtopic": case "tangent": return ModerationFlag.Tangent;
                case "informative": return ModerationFlag.Informative;
                case "nuked": return ModerationFlag.Nuked;
                case "cortex": return ModerationFlag.Cortex;
                default:
                    Console.WriteLine($"Unknown ModerationFlag: {str}");
                    return ModerationFlag.OnTopic;
            }
        }
    }
}
