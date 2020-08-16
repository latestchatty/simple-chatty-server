using Microsoft.AspNetCore.Mvc;

namespace SimpleChattyServer.Data.Requests
{
    public sealed class V1PostRequest
    {
        [FromForm(Name = "parent_id")] public string ParentId { get; set; }
        public string Body { get; set; }
    }
}
