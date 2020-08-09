using System;

namespace SimpleChattyServer.Responses
{
    public sealed class GetNewestPostInfoResponse
    {
        public int Id { get; set; }
        public DateTimeOffset Date { get; set; }
    }
}
