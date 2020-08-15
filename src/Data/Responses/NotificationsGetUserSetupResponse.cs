using System.Collections.Generic;

namespace SimpleChattyServer.Data.Responses
{
    public sealed class NotificationsGetUserSetupResponse
    {
        public bool TriggerOnReply { get; set; } = false;
        public bool TriggerOnMention { get; set; } = false;
        public List<string> TriggerKeywords { get; set; } = new List<string>();
    }
}
