namespace SimpleChattyServer.Data.Requests
{
    public sealed class SetCategoryFiltersRequest
    {
        public string Username { get; set; }
        public bool Nws { get; set; }
        public bool Stupid { get; set; }
        public bool Political { get; set; }
        public bool Tangent { get; set; }
        public bool Informative { get; set; }
    }
}
