using SimpleChattyServer.Data;

namespace SimpleChattyServer.Data.Responses
{
    public sealed class GetCategoryFiltersResponse
    {
        public CategoryFilters Filters { get; set; }

        public sealed class CategoryFilters
        {
            public bool Nws { get; set; }
            public bool Stupid { get; set; }
            public bool Political { get; set; }
            public bool Tangent { get; set; }
            public bool Informative { get; set; }
        }
    }
}
