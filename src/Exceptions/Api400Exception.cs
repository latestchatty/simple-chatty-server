namespace SimpleChattyServer.Exceptions
{
    public sealed class Api400Exception : ApiException
    {
        public static class Codes
        {
            public const string ARGUMENT = "ERR_ARGUMENT";
        }

        public Api400Exception(string code, string message) : base(code, message)
        {
        }
    }
}
