namespace SimpleChattyServer.Exceptions
{
    public sealed class Api500Exception : ApiException
    {
        public static class Codes
        {
            public const string SERVER = "ERR_SERVER";
        }

        public Api500Exception(string message) : base(Codes.SERVER, message)
        {
        }

        public Api500Exception(string code, string message) : base(code, message)
        {
        }
    }
}
