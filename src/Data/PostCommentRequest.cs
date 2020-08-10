namespace SimpleChattyServer.Data
{
    public sealed class PostCommentRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int ParentId { get; set; }
        public string Text { get; set; }
    }
}
