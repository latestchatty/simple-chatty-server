namespace SimpleChattyServer.Data.Requests
{
    public sealed class SetPostCategoryRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int PostId { get; set; }
        public string Category { get; set; }
    }
}
