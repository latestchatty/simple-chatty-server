namespace SimpleChattyServer.Data.Requests
{
    public sealed class SetClientDataRequest
    {
        public string Username { get; set; }
        public string Client { get; set; }
        public string Data { get; set; }
    }
}
