using System.Collections.Generic;

namespace SimpleChattyServer.Data
{
    public sealed class UserData
    {
        public bool FilterNws { get; set; } = true;
        public bool FilterStupid { get; set; } = true;
        public bool FilterPolitical { get; set; } = true;
        public bool FilterTangent { get; set; } = true;
        public bool FilterInformative { get; set; } = true;
        public List<MarkedPostModel> MarkedPosts { get; set; } = new List<MarkedPostModel>();
        public Dictionary<string, string> ClientData { get; set; } = new Dictionary<string, string>();
    }
}
