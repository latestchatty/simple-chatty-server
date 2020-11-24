using System;

namespace SimpleChattyServer.Data
{
    public class CortexUserData
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public int Points { get; set; }
        public int Comments { get; set; }
        public int CortexPosts { get; set; }
        public int Wins { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}