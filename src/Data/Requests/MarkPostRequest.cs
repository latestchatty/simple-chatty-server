namespace SimpleChattyServer.Data.Requests
{
    public sealed class MarkPostRequest
    {
        public string Username { get; set; }
        public int PostId { get; set; }
        public string Type { get; set; }
    }
}
