namespace SimpleChattyServer.Data.Requests
{
    public sealed class GetMessagesRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Folder { get; set; }
        public int Page { get; set; }
    }
}
