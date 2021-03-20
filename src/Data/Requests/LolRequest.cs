namespace SimpleChattyServer.Data.Requests
{
    public sealed class LolRequest
    {
        public int What { get; set; }
        public string Who { get; set; }
        public string Tag { get; set; }
        public string Action { get; set; }
        public string Password { get; set; }
    }
}
