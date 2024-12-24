namespace SimpleChattyServer.Data.Options
{
    public sealed class DukeNukedOptions
    {
        public const string SectionName = "DukeNuked";

        public bool Enabled { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SlackToken { get; set; }
    }
}
