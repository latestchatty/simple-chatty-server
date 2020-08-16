namespace SimpleChattyServer.Data.Requests
{
    public sealed class SetReadStatusRequest
    {
        public string Username { get; set; }
        public int ThreadId { get; set; }
        public int LastReadPostId { get; set; }
    }
}
