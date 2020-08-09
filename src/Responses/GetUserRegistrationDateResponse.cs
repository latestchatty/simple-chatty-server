using System;
using System.Collections.Generic;

namespace SimpleChattyServer.Responses
{
    public sealed class GetUserRegistrationDateResponse
    {
        public List<User> Users { get; set; }

        public sealed class User
        {
            public string Username { get; set; }
            public DateTimeOffset Date { get; set; }
        }
    }
}
