namespace SimpleChattyServer.Data.Options
{
    public sealed class SharedLoginOptions
    {
        public const string SectionName = "SharedLogin";

        public string Username { get; set; }
        public string Password { get; set; }
        public string LolApiSecret { get; set; }
    }
}
