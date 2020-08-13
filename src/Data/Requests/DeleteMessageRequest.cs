namespace SimpleChattyServer.Data.Requests
{
    public sealed class DeleteMessageRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int MessageId { get; set; }
        public string Folder { get; set; }
    }
}
