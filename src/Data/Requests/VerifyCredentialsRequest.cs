namespace SimpleChattyServer.Data.Requests
{
    public sealed class VerifyCredentialsRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
