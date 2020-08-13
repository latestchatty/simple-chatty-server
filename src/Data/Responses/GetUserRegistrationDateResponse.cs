using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Data.Responses
{
    public sealed class GetUserRegistrationDateResponse
    {
        public List<User> Users { get; set; }

        public sealed class User
        {
            public string Username { get; set; }
            [JsonConverter(typeof(V2DateTimeOffsetConverter))] public DateTimeOffset Date { get; set; }
        }
    }
}
