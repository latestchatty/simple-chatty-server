using System.Text.Json;

namespace SimpleChattyServer.Data
{
    public sealed class NewPostEventDataModel : EventDataModel
    {
        public int PostId { get; set; }
        public PostModel Post { get; set; }
        public string ParentAuthor { get; set; }

        public override void Write(Utf8JsonWriter writer) =>
            JsonSerializer.Serialize(writer, this, _options);
    }
}
