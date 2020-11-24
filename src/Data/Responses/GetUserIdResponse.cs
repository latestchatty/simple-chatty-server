namespace SimpleChattyServer.Data.Responses
{
    public sealed class GetUserIdResponse
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public GetUserIdResponse(int userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }
    }
}