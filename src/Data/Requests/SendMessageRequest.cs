namespace SimpleChattyServer.Data.Requests
{
    public sealed class SendMessageRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}
