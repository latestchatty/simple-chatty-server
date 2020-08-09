using System.Collections.Generic;

namespace SimpleChattyServer.Data
{
    public sealed class LolCountsUpdateEventDataModel : EventDataModel
    {
        public List<LolCountUpdateModel> Updates { get; set; }
    }
}
