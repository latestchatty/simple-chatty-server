using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SimpleChattyServer
{
    public sealed class RequestLogMiddleware
    {
        private const string REQUEST_TEMPLATE = "{RequestMethod} {RequestPath} {StatusCode} {Elapsed:#,##0} ms";

        private readonly ILogger _logger;
        private readonly RequestDelegate _next;

        public RequestLogMiddleware(ILogger<RequestLogMiddleware> logger, RequestDelegate next)
        {
            _logger = logger;
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var sw = Stopwatch.StartNew();
            await _next(httpContext);

            var statusCode = httpContext.Response?.StatusCode;
            if (statusCode >= 500)
            {
                _logger.LogError(REQUEST_TEMPLATE, httpContext.Request.Method, httpContext.Request.Path, statusCode,
                    sw.Elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation(REQUEST_TEMPLATE, httpContext.Request.Method, httpContext.Request.Path,
                    statusCode, sw.Elapsed.TotalMilliseconds);
            }
        }
    }
}
