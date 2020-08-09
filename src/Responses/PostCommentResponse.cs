namespace SimpleChattyServer.Responses
{
    public sealed class PostCommentResponse
    {
        public string Result { get; set; } = "success";
        public int NewPostId { get; set; }
    }
}
