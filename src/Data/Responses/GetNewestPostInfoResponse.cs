using System;
using System.Text.Json.Serialization;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Data.Responses
{
    public sealed class GetNewestPostInfoResponse
    {
        public int Id { get; set; }
        [JsonConverter(typeof(V2DateTimeOffsetConverter))] public DateTimeOffset Date { get; set; }
    }
}
