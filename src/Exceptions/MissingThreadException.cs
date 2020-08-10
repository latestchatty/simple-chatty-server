namespace SimpleChattyServer.Exceptions
{
    public sealed class MissingThreadException : Api400Exception
    {
        public MissingThreadException(string message) : base(Api400Exception.Codes.INVALID_POST, message)
        {
        }
    }
}
