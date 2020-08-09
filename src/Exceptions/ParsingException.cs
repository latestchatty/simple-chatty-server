using System;

namespace SimpleChattyServer.Exceptions
{
    public sealed class ParsingException : Exception
    {
        public ParsingException(string message) : base(message)
        {
        }
    }
}
