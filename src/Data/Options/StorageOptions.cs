namespace SimpleChattyServer.Data.Options
{
    public sealed class StorageOptions
    {
        public const string SectionName = "Storage";

        public string DataPath { get; set; }
        public string LogPath { get; set; }
    }
}
