using System.Collections.Generic;
using System.Text.Json;

namespace SimpleChattyServer.Data
{
    public sealed class LolCountsUpdateEventDataModel : EventDataModel
    {
        public List<LolCountUpdateModel> Updates { get; set; }

        public override void Write(Utf8JsonWriter writer) =>
            JsonSerializer.Serialize(writer, this, _options);
    }
}
