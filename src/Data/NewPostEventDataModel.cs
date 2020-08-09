namespace SimpleChattyServer.Data
{
    public sealed class NewPostEventDataModel : EventDataModel
    {
        public int PostId { get; set; }
        public PostModel Post { get; set; }
        public string ParentAuthor { get; set; }
    }
}
