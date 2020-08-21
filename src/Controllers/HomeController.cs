using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SimpleChattyServer.Data;
using SimpleChattyServer.Services;

namespace SimpleChattyServer.Controllers
{
    public sealed class HomeController : ControllerBase
    {
        private readonly SearchParser _searchParser;

        public HomeController(SearchParser searchParser)
        {
            _searchParser = searchParser;
        }

        [HttpGet]
        public ContentResult Index()
        {
            Response.StatusCode = 301;
            Response.Headers["Location"] = "/search";
            return Content("");
        }

        [HttpGet("search")]
        public async Task<ContentResult> Search(string terms = "", string author = "", string parentAuthor = "",
            string category = "", int page = 1)
        {
            terms = terms ?? "";
            author = author ?? "";
            parentAuthor = parentAuthor ?? "";
            category = category ?? "";
            var perPage = 15;
            var sb = new StringBuilder();
            sb.Append(string.Format(SEARCH_HEADER,
                WebUtility.HtmlEncode(terms).Replace("\"", "&quot;"),
                WebUtility.HtmlEncode(author).Replace("\"", "&quot;"),
                WebUtility.HtmlEncode(parentAuthor).Replace("\"", "&quot;"),
                category == "nws" ? "selected" : "",
                category == "informative" ? "selected" : ""));
            if (terms != "" || author != "" || parentAuthor != "" || category != "")
            {
                var results = await _searchParser.Search(terms, author, parentAuthor, category, page);
                sb.Append(string.Format(SEARCH_RESULTS_HEADER,
                    /* {0} */ WebUtility.HtmlEncode(terms).Replace("\"", "&quot;"),
                    /* {1} */ WebUtility.HtmlEncode(author).Replace("\"", "&quot;"),
                    /* {2} */ WebUtility.HtmlEncode(parentAuthor).Replace("\"", "&quot;"),
                    /* {3} */ WebUtility.HtmlEncode(category).Replace("\"", "&quot;"),
                    /* {4} */ page == 1 ? "disabled" : "",
                    /* {5} */ page > 1 ? $"{page - 1}" : "1",
                    /* {6} */ $"{page + 1}",
                    /* {7} */ results.Results.Count < perPage ? "disabled" : "",
                    /* {8} */ $"{(int)Math.Ceiling((double)results.TotalResults / perPage)}",
                    /* {9} */ $"{results.TotalResults:#,##0}",
                    /* {10} */ $"{page}"));
                if (results.Results.Count == 0)
                {
                    sb.Append(NO_SEARCH_RESULTS);
                }
                foreach (var result in results.Results)
                {
                    var pptTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(result.Date, PacificTimeZone.TimeZoneId);
                    var timeZoneAbbreviation = PacificTimeZone.GetAbbreviationFromOffset(pptTime.Offset);
                    sb.Append(string.Format(SEARCH_RESULT,
                        /* {0} */ $"{result.Id}",
                        /* {1} */ result.Preview,
                        /* {2} */ WebUtility.HtmlEncode(result.Author),
                        /* {3} */ $"{pptTime:M/d/yyyy h:mm tt} {timeZoneAbbreviation}"));
                }
                sb.Append(SEARCH_RESULTS_FOOTER);
            }
            sb.Append(SEARCH_FOOTER);
            return Content(sb.ToString(), "text/html");
        }

        private const string SEARCH_HEADER = @"
<!DOCTYPE html>
<html>
    <head>
        <meta charset=""utf-8""> 
        <title>WinChatty Search</title>
        <style>
            *                 {{ font-family: sans-serif; }}
            body              {{ overflow-y: scroll; margin: 0; font-size: 16px; min-width: 900px; }}
            a.link            {{ text-decoration: none; color: #051047; }}
            a:hover           {{ text-decoration: underline; }}
            .text             {{ border: 1px solid #C6C6C6; padding: 2px; font-size: 16px; }}
            .button           {{ height: 28px; width: 75px; font-size: 16px; }}
            #formTable        {{ margin: 0 auto; border-spacing: 0; padding: 0; }}
            #formHeader td    {{ font-size: 12px; color: gray; padding: 0px; padding-left: 4px; }}
            #category         {{ border: 1px solid #C6C6C6; padding: 1px; font-size: 16px; }}
            #formContainer    {{ background: #EEEEEE; border-bottom: 1px solid gray; padding: 5px; height: 46px; }}
            .body             {{ padding-right: 5px; padding-bottom: 5px; font-size: 14px; width: 495px; max-width: 495px; overflow: hidden; 
                                text-overflow: ellipsis; white-space: nowrap; }}
            .author           {{ padding-right: 5px; padding-bottom: 5px; font-size: 14px; width: 100px; max-width: 100px; overflow: hidden; 
                                text-overflow: ellipsis; white-space: nowrap; }}
            .date             {{ padding-bottom: 5px; font-size: 14px; width: 150px; max-width: 100px; overflow: hidden; 
                                text-overflow: ellipsis; white-space: nowrap; }}
            .resultLink       {{ color: black; text-decoration: none; }}
            #results          {{ margin: 0 30px; max-width: 1200px; }} 
            #results > table  {{ width: 100%; }}
        </style>
    </head>
    <body>
        <div id=""formContainer"" style=""position: fixed; width: 100%;"">
            <form action=""/search"" method=""get"">
                <table id=""formTable"">
                    <tr id=""formHeader"">
                        <td>Search:</td>
                        <td>Author:</td>
                        <td>Parent author:</td>
                        <td>Moderation flag:</td>
                        <td></td>
                    </tr>
                    <tr>
                        <td><input class=""text"" autofocus type=""text"" name=""terms"" value=""{0}""></td>
                        <td><input class=""text"" type=""text"" name=""author"" value=""{1}""></td>
                        <td><input class=""text"" type=""text"" name=""parentAuthor"" value=""{2}""></td>
                        <td>
                            <select name=""category"" id=""category"">
                                <option></option>
                                <option {3}>nws</option>
                                <option {4}>informative</option>
                            </select>
                        </td>
                        <td><input type=""submit"" value=""Search"" class=""button"" id=""btnSubmit""></td>
                    </tr>
                </table>
            </form>
        </div>
";

        private const string SEARCH_RESULTS_HEADER = @"
        <div style=""height: 70px;""></div>
        <div style=""font-size: 16px; padding: 10px; float: left;"">
        Found {9} total results.
        </div>
        <div style=""text-align: right; float: right;"">
            <form method=""get"" action=""search"" style=""display: inline;"">
                <input type=""hidden"" name=""terms"" value=""{0}"">
                <input type=""hidden"" name=""author"" value=""{1}"">
                <input type=""hidden"" name=""parentAuthor"" value=""{2}"">
                <input type=""hidden"" name=""category"" value=""{3}"">
                <input type=""hidden"" name=""page"" value=""1"">
                <input class=""button"" type=""submit"" value=""|&laquo;"" {4}>
            </form>
            <form method=""get"" action=""search"" style=""display: inline;"">
                <input type=""hidden"" name=""terms"" value=""{0}"">
                <input type=""hidden"" name=""author"" value=""{1}"">
                <input type=""hidden"" name=""parentAuthor"" value=""{2}"">
                <input type=""hidden"" name=""category"" value=""{3}"">
                <input type=""hidden"" name=""page"" value=""{5}"">
                <input class=""button"" type=""submit"" value=""&laquo; Back"" {4}>
            </form>
            <span style=""font-size: 16px; padding: 10px;"">Page {10}</span>
            <form method=""get"" action=""search"" style=""display: inline;"">
                <input type=""hidden"" name=""terms"" value=""{0}"">
                <input type=""hidden"" name=""author"" value=""{1}"">
                <input type=""hidden"" name=""parentAuthor"" value=""{2}"">
                <input type=""hidden"" name=""category"" value=""{3}"">
                <input type=""hidden"" name=""page"" value=""{6}"">
                <input class=""button"" type=""submit"" value=""Next &raquo;"" {7}>
            </form>
            <form method=""get"" action=""search"" style=""display: inline;"">
                <input type=""hidden"" name=""terms"" value=""{0}"">
                <input type=""hidden"" name=""author"" value=""{1}"">
                <input type=""hidden"" name=""parentAuthor"" value=""{2}"">
                <input type=""hidden"" name=""category"" value=""{3}"">
                <input type=""hidden"" name=""page"" value=""{8}"">
                <input class=""button"" type=""submit"" value=""&raquo;|"" {7}>
            </form>
        </div>   
        <div id=""results"">
            <table style=""margin: 0 auto; margin-bottom: 50px;""><tbody>
";

        private const string NO_SEARCH_RESULTS = @"
                <tr>
                    <td>
                    No matching comments were found.
                    </td>
                </tr>
";

        private const string SEARCH_RESULT = @"
                <tr>
                    <td class=""body""><a class=""resultLink"" href=""http://www.shacknews.com/chatty?id={0}#item_{0}"">{1}</a></td>
                    <td class=""author"">{2}</td>
                    <td class=""date"">{3}</td>
                </tr>
";

        private const string SEARCH_RESULTS_FOOTER = @"
            </table>
        </div>
";

        private const string SEARCH_FOOTER = @"
    </body>
</html>
";
    }
}
