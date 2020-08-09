using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class MailboxConverter : JsonConverter<Mailbox>
    {
        public override Mailbox Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Parse(reader.GetString());

        public override void Write(Utf8JsonWriter writer, Mailbox value, JsonSerializerOptions options) =>
            writer.WriteStringValue(ToJsonString(value));

        private static string ToJsonString(Mailbox self)
        {
            switch (self)
            {
                case Mailbox.Inbox: return "inbox";
                case Mailbox.Sent: return "sent";
                default: throw new ArgumentOutOfRangeException(nameof(self));
            }
        }

        public static Mailbox Parse(string str)
        {
            switch (str)
            {
                case "inbox": return Mailbox.Inbox;
                case "sent": return Mailbox.Sent;
                default: throw new ArgumentOutOfRangeException(str);
            }
        }
    }
}
