using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Options;
using SimpleChattyServer.Options;

namespace SimpleChattyServer.Services
{
    public sealed class DownloadService
    {
        private readonly SharedLoginOptions _sharedLoginOptions;
        private readonly Encoding _utf8Encoding;

        private IReadOnlyList<Cookie> _sharedLoginCookies;

        public DownloadService(IOptions<SharedLoginOptions> sharedLoginOptions)
        {
            _sharedLoginOptions = sharedLoginOptions.Value;
            _utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }

        public async Task<string> DownloadWithSharedLogin(string url, bool verifyLoginStatus = true,
            string postBody = null)
        {
            var html = await DownloadWithExistingSharedLoginCookies(url, verifyLoginStatus, postBody);
            if (html != null)
                return html;

            await LogIntoSharedAccount();

            html = await DownloadWithExistingSharedLoginCookies(url, verifyLoginStatus, postBody);
            if (html != null)
                return html;

            throw new Exception("Unable to log into the shared user account.");
        }

        public async Task<string> DownloadWithUserLogin(
            string url, string username, string password,
            string postBody = null)
        {
            var cookies = await LogIn(username, password);
            var request = CreateRequest(url, cookies, method: postBody == null ? "GET" : "POST");

            if (postBody != null)
                await WriteRequestBody(request, postBody);

            return await GetResponse(request);
        }

        public NameValueCollection NewQuery() => HttpUtility.ParseQueryString("");

        private static HttpWebRequest CreateRequest(
            string url, IEnumerable<Cookie> cookies = null, string requestedWith = "libcurl", string method = "GET")
        {
            var request = WebRequest.CreateHttp(url);
            request.Method = method;
            request.AllowAutoRedirect = true;
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.UserAgent = "SimpleChattyServer";
            request.Headers.Add("X-Requested-With", requestedWith);
            request.Timeout = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
            request.CookieContainer = new CookieContainer();

            if (cookies != null)
                foreach (var cookie in cookies)
                    request.CookieContainer.Add(cookie);

            return request;
        }

        private static async Task<string> GetResponse(HttpWebRequest request)
        {
            using var response = await request.GetResponseAsync();
            using var stream = response.GetResponseStream();
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private async Task<string> DownloadWithExistingSharedLoginCookies(string url, bool verifyLoginStatus,
            string postBody)
        {
            if (_sharedLoginCookies == null)
                return null; // haven't logged in at all yet

            var request = CreateRequest(url, _sharedLoginCookies, method: postBody == null ? "GET" : "POST");

            if (postBody != null)
                await WriteRequestBody(request, postBody);

            var html = await GetResponse(request);

            if (!verifyLoginStatus || html.Contains("<li style=\"display: none\" id=\"user_posts\">"))
                return html; // we're already logged in
            else
                return null; // nope, we're not logged in
        }

        private async Task LogIntoSharedAccount()
        {
            _sharedLoginCookies = await LogIn(_sharedLoginOptions.Username, _sharedLoginOptions.Password);
        }

        private async Task<IReadOnlyList<Cookie>> LogIn(string username, string password)
        {
            var query = NewQuery();
            query.Add("get_fields[]", "result");
            query.Add("user-identifier", username);
            query.Add("supplied-pass", password);
            query.Add("remember-login", "1");

            var queryString = query.ToString();

            var request = CreateRequest("https://www.shacknews.com/account/signin",
                requestedWith: "XMLHttpRequest", method: "POST");
            await WriteRequestBody(request, query.ToString());

            using var response = (HttpWebResponse)await request.GetResponseAsync();
            using var responseStream = response.GetResponseStream();
            using var responseStreamReader = new StreamReader(responseStream);
            var html = await responseStreamReader.ReadToEndAsync();

            if (html.Contains("{\"result\":{\"valid\":\"true\""))
                return response.Cookies.Cast<Cookie>().ToList();
            else
                throw new Exception("Unable to log into the user account.");
        }

        private async Task WriteRequestBody(HttpWebRequest request, string query)
        {
            request.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            using var requestStream = await request.GetRequestStreamAsync();
            using var requestStreamWriter = new StreamWriter(requestStream, _utf8Encoding);
            requestStreamWriter.Write(query);
        }
    }
}
