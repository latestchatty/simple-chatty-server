using System;

namespace SimpleChattyServer.Data
{
    public class UserFlair
    {
        public bool IsBriefcase { get; set; }
        public bool IsModerator { get; set; }
        public bool IsTenYear { get; set; }
        public bool IsTwentyYear { get; set; }
        public MercuryStatus MercuryStatus { get; set; }
    }
}