namespace SimpleChattyServer.Responses
{
    public sealed class VerifyCredentialsResponse
    {
        public bool IsValid { get; set; }
        public bool IsModerator { get; set; }
    }
}
