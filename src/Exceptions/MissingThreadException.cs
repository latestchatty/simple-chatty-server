using System;

namespace SimpleChattyServer.Exceptions
{
    public sealed class MissingThreadException : Exception
    {
        public MissingThreadException(string message) : base(message)
        {
        }
    }
}
