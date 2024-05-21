namespace SimpleChattyServer.Data.Responses
{
   public sealed class PostCommentResponse : SuccessResponse
   {
      public PostModel Post { get; set; }
   }
}
