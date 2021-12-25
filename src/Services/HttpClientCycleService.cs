using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimpleChattyServer.Services
{
    public sealed class HttpClientCycleService : IHostedService, IDisposable
    {
        private readonly Timer _timer;
        private readonly ILogger<HttpClientCycleService> _logger;
        private readonly DownloadService _downloadService;

        public HttpClientCycleService(
            ILogger<HttpClientCycleService> logger,
            DownloadService downloadService)
        {
            _timer = new(Cycle, null, Timeout.Infinite, Timeout.Infinite);
            _logger = logger;
            _downloadService = downloadService;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Change(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            return Task.CompletedTask;
        }

        private void Cycle(object state)
        {
            _logger.LogInformation("Cycling HTTP clients.");
            _downloadService.CycleSharedHttpClients()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
    }
}
