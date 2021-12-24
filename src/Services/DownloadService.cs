using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Options;
using SimpleChattyServer.Data.Options;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Services
{
    public sealed class DownloadService : IDisposable
    {
        private readonly HttpClient _anonymousHttpClient = CreateHttpClient(timeout: 30);
        private readonly HttpClient _sharedHttpClient = CreateHttpClient(timeout: 30);
        private readonly SharedLoginOptions _sharedLoginOptions;
        private readonly Encoding _utf8Encoding;

        public DownloadService(IOptions<SharedLoginOptions> sharedLoginOptions)
        {
            _sharedLoginOptions = sharedLoginOptions.Value;
            _utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }

        public async Task<string> DownloadWithSharedLogin(string url, bool verifyLoginStatus = true,
            Dictionary<string, string> postBody = null)
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
            Dictionary<string, string> postBody = null)
        {
            using var client = CreateHttpClient(timeout: 30);
            await LogIn(username, password, client);
            using var request = CreateRequest(
                url,
                method: postBody == null ? HttpMethod.Get : HttpMethod.Post,
                requestBody: postBody);
            using var response = await client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> DownloadAnonymous(string url, Dictionary<string, string> postBody = null)
        {
            using var request = CreateRequest(url, method: postBody == null ? HttpMethod.Get : HttpMethod.Post,
                requestBody: postBody);
            using var response = await _anonymousHttpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        public Dictionary<string, string> NewQuery() => new();

        // Caller must dispose the returned client.
        private static HttpClient CreateHttpClient(int timeout) =>
            new(
                handler: new HttpClientHandler()
                {
                    CookieContainer = new(),
                    AllowAutoRedirect = true,
                    AutomaticDecompression = DecompressionMethods.GZip,
                },
                disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(timeout),
            };

        // Caller must dispose the returned message.
        private static HttpRequestMessage CreateRequest(
            string url, string requestedWith = "libcurl", HttpMethod method = null,
            Dictionary<string, string> requestBody = null)
        {
            method ??= HttpMethod.Get;

            HttpRequestMessage request = new(method, url);
            request.Headers.Add("User-Agent", "SimpleChattyServer");
            request.Headers.Add("X-Requested-With", requestedWith);

            if (requestBody != null)
                request.Content = new FormUrlEncodedContent(requestBody);

            return request;
        }

        private async Task<string> DownloadWithExistingSharedLoginCookies(string url, bool verifyLoginStatus,
            Dictionary<string, string> postBody)
        {
            using var request = CreateRequest(url, method: postBody == null ? HttpMethod.Get : HttpMethod.Post,
                requestBody: postBody);
            using var response = await _sharedHttpClient.SendAsync(request);
            var html = await response.Content.ReadAsStringAsync();

            if (!verifyLoginStatus || html.Contains("<li style=\"display: none\" id=\"user_posts\">"))
                return html; // we're already logged in
            else
                return null; // nope, we're not logged in
        }

        private Task LogIntoSharedAccount() =>
            LogIn(
                _sharedLoginOptions.Username,
                _sharedLoginOptions.Password,
                _sharedHttpClient);

        private async Task LogIn(string username, string password, HttpClient client)
        {
            var query = HttpUtility.ParseQueryString("");
            query.Add("get_fields[]", "result");
            query.Add("user-identifier", username);
            query.Add("supplied-pass", password);
            query.Add("remember-login", "1");

            using HttpRequestMessage request =
                new(HttpMethod.Post, "https://www.shacknews.com/account/signin")
                {
                    Content = new StringContent(
                        query.ToString(),
                        _utf8Encoding,
                        "application/x-www-form-urlencoded")
                };

            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            using var response = await client.SendAsync(request);
            var html = await response.Content.ReadAsStringAsync();

            if (!html.Contains("{\"result\":{\"valid\":\"true\""))
                throw new Api400Exception(Api400Exception.Codes.INVALID_LOGIN,
                    $"Unable to log into the user account [{username}].");
        }

        public void Dispose()
        {
            _anonymousHttpClient?.Dispose();
            _sharedHttpClient?.Dispose();
        }
    }
}
