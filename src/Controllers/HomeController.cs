using Microsoft.AspNetCore.Mvc;

namespace SimpleChattyServer.Controllers
{
    public sealed class HomeController : ControllerBase
    {
        [HttpGet]
        public ContentResult Index()
        {
            Response.StatusCode = 301;
            Response.Headers["Location"] = "https://www.shacknews.com/search?q=&type=4";
            return Content("");
        }

        [HttpGet("search")]
        public ContentResult Search()
        {
            Response.StatusCode = 301;
            Response.Headers["Location"] = "https://www.shacknews.com/search?q=&type=4";
            return Content("");
        }
    }
}
