namespace SimpleChattyServer.Data.Responses
{
    public sealed class GetMessageCountResponse
    {
        public int Total { get; set; }
        public int Unread { get; set; }
    }
}
