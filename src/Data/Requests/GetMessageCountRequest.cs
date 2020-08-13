namespace SimpleChattyServer.Data.Requests
{
    public sealed class GetMessageCountRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
