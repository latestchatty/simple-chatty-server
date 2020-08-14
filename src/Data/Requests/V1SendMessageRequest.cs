namespace SimpleChattyServer.Data.Requests
{
    public sealed class V1SendMessageRequest
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}
