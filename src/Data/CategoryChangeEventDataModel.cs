namespace SimpleChattyServer.Data
{
    public sealed class CategoryChangeEventDataModel : EventDataModel
    {
        public int PostId { get; set; }
        public ModerationFlag Category { get; set; }
    }
}
