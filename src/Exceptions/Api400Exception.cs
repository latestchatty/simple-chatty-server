namespace SimpleChattyServer.Exceptions
{
    public class Api400Exception : ApiException
    {
        public static class Codes
        {
            public const string ARGUMENT = "ERR_ARGUMENT";
            public const string BANNED = "ERR_BANNED";
            public const string INVALID_LOGIN = "ERR_INVALID_LOGIN";
            public const string INVALID_PARENT = "ERR_INVALID_PARENT";
            public const string INVALID_POST = "ERR_INVALID_POST";
            public const string NOT_MODERATOR = "ERR_NOT_MODERATOR";
            public const string NUKED = "ERR_NUKED";
            public const string POST_RATE_LIMIT = "ERR_POST_RATE_LIMIT";
        }

        public Api400Exception(string message) : base(Codes.ARGUMENT, message)
        {
        }

        public Api400Exception(string code, string message) : base(code, message)
        {
        }
    }
}
