using System;

namespace SimpleChattyServer.Exceptions
{
    public abstract class ApiException : Exception
    {
        public string Code { get; }

        public ApiException(string code, string message) : base(message)
        {
            Code = code;
        }
    }
}
