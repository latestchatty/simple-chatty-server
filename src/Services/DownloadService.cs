using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleChattyServer.Data.Options;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Services
{
    public static class QueryExtensions
    {
        public static void Add(this List<KeyValuePair<string, string>> self, string key, string value)
        {
            self.Add(new(key, value));
        }
    }

    public sealed class DownloadService : IDisposable
    {
        private HttpClient _anonymousHttpClient = CreateHttpClient();
        private readonly LoggedReaderWriterLock _anonymousHttpClientLock;
        private HttpClient _sharedHttpClient = CreateHttpClient();
        private readonly LoggedReaderWriterLock _sharedHttpClientLock;
        private readonly SharedLoginOptions _sharedLoginOptions;
        private readonly Encoding _utf8Encoding;

        public DownloadService(
            ILogger<DownloadService> logger,
            IOptions<SharedLoginOptions> sharedLoginOptions)
        {
            _anonymousHttpClientLock = new("Anonymous HttpClient", x => logger.LogDebug(x));
            _sharedHttpClientLock = new("Shared HttpClient", x => logger.LogDebug(x));
            _sharedLoginOptions = sharedLoginOptions.Value;
            _utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }

        public async Task CycleSharedHttpClients()
        {
            await _anonymousHttpClientLock.WithWriteLock(nameof(CycleSharedHttpClients), () =>
            {
                _anonymousHttpClient.Dispose();
                _anonymousHttpClient = CreateHttpClient();
            });

            await _sharedHttpClientLock.WithWriteLock(nameof(CycleSharedHttpClients), () =>
            {
                _sharedHttpClient.Dispose();
                _sharedHttpClient = CreateHttpClient();
            });
        }

        public async Task<string> DownloadWithSharedLogin(string url, bool verifyLoginStatus = true,
            IEnumerable<KeyValuePair<string, string>> postBody = null)
        {
            var html = await _sharedHttpClientLock.WithReadLock(nameof(DownloadWithSharedLogin), () =>
                DownloadWithExistingSharedLoginCookies(url, verifyLoginStatus, postBody));
            if (html != null)
                return html;

            await _sharedHttpClientLock.WithWriteLock(nameof(DownloadWithSharedLogin), async () =>
            {
                // Another concurrent task might have successfully logged in while we were waiting for the lock, so
                // let's try it one more time without logging in.
                html = await DownloadWithExistingSharedLoginCookies(url, verifyLoginStatus, postBody);
                if (html != null)
                    return;

                // Nope, it's up to us to log in.
                await LogIntoSharedAccount();
            });

            html = await _sharedHttpClientLock.WithReadLock(nameof(DownloadWithSharedLogin), () =>
                DownloadWithExistingSharedLoginCookies(url, verifyLoginStatus, postBody));
            if (html != null)
                return html;

            // Give up.
            throw new Exception("Unable to log into the shared user account.");
        }

        public async Task<string> DownloadWithUserLogin(
            string url, string username, string password,
            IEnumerable<KeyValuePair<string, string>> postBody = null)
        {
            using var client = CreateHttpClient();
            await LogIn(username, password, client);
            using var request = CreateRequest(
                url,
                method: postBody == null ? HttpMethod.Get : HttpMethod.Post,
                requestBody: postBody);
            using var response = await client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> DownloadAnonymous(string url, IEnumerable<KeyValuePair<string, string>> postBody = null)
        {
            using var request = CreateRequest(url, method: postBody == null ? HttpMethod.Get : HttpMethod.Post,
                requestBody: postBody);
            return await _anonymousHttpClientLock.WithReadLock(nameof(DownloadAnonymous), async () =>
            {
                using var response = await _anonymousHttpClient.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            });
        }

        public List<KeyValuePair<string, string>> NewQuery() => new();

        // Caller must dispose the returned client.
        private static HttpClient CreateHttpClient() =>
            new(
                handler: new HttpClientHandler()
                {
                    CookieContainer = new(),
                    AllowAutoRedirect = true,
                    AutomaticDecompression = DecompressionMethods.GZip,
                    MaxConnectionsPerServer = 32,
                },
                disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(30),
            };

        // Caller must dispose the returned message.
        private static HttpRequestMessage CreateRequest(
            string url, string requestedWith = "libcurl", HttpMethod method = null,
            IEnumerable<KeyValuePair<string, string>> requestBody = null)
        {
            method ??= HttpMethod.Get;

            HttpRequestMessage request = new(method, url);
            request.Headers.Add("User-Agent", "SimpleChattyServer");
            request.Headers.Add("X-Requested-With", requestedWith);

            if (requestBody != null)
                request.Content = new FormUrlEncodedContent(requestBody);

            return request;
        }

        // Caller must hold shared HttpClient lock
        private async Task<string> DownloadWithExistingSharedLoginCookies(string url, bool verifyLoginStatus,
            IEnumerable<KeyValuePair<string, string>> postBody)
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

        // Caller must hold shared HttpClient lock
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
