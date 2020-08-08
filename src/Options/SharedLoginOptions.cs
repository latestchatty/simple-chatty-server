namespace SimpleChattyServer.Options
{
    public sealed class SharedLoginOptions
    {
        public const string SectionName = "SharedLogin";

        public string Username { get; set; }
        public string Password { get; set; }
    }
}
