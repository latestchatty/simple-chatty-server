namespace SimpleChattyServer.Data.Requests
{
    public sealed class MarkMessageReadRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int MessageId { get; set; }
    }
}
