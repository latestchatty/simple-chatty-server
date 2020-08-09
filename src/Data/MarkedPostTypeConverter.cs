using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class MarkedPostTypeConverter : JsonConverter<MarkedPostType>
    {
        public override MarkedPostType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Parse(reader.GetString());

        public override void Write(Utf8JsonWriter writer, MarkedPostType value, JsonSerializerOptions options) =>
            writer.WriteStringValue(ToJsonString(value));

        private static string ToJsonString(MarkedPostType self)
        {
            switch (self)
            {
                case MarkedPostType.Unmarked: return "unmarked";
                case MarkedPostType.Pinned: return "pinned";
                case MarkedPostType.Collapsed: return "collapsed";
                default: throw new ArgumentOutOfRangeException(nameof(self));
            }
        }

        public static MarkedPostType Parse(string str)
        {
            switch (str)
            {
                case "unmarked": return MarkedPostType.Unmarked;
                case "pinned": return MarkedPostType.Pinned;
                case "collapsed": return MarkedPostType.Collapsed;
                default: throw new ArgumentOutOfRangeException(str);
            }
        }
    }
}
