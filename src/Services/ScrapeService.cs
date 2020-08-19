using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Services
{
    public sealed class ScrapeService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ChattyParser _chattyParser;
        private readonly ThreadParser _threadParser;
        private readonly LolParser _lolParser;
        private readonly DownloadService _downloadService;
        private readonly ChattyProvider _chattyProvider;
        private readonly EventProvider _eventProvider;
        private readonly Timer _timer;
        private readonly Dictionary<int, (string Html, ChattyPage Page)> _previousPages =
            new Dictionary<int, (string Html, ChattyPage Page)>();
        private (string Json, ChattyLolCounts LolCounts) _previousLols = (null, null);

        public ScrapeService(ILogger<ScrapeService> logger, ChattyParser chattyParser, ThreadParser threadParser,
            LolParser lolParser, DownloadService downloadService, ChattyProvider chattyProvider,
            EventProvider eventProvider)
        {
            _logger = logger;
            _chattyParser = chattyParser;
            _threadParser = threadParser;
            _lolParser = lolParser;
            _downloadService = downloadService;
            _chattyProvider = chattyProvider;
            _eventProvider = eventProvider;
            _timer = new Timer(Scrape, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartTimer(runImmediately: true);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopTimer();
            return Task.CompletedTask;
        }

        private void StartTimer(bool runImmediately) =>
            _timer.Change(runImmediately ? TimeSpan.Zero : TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        private void StopTimer() =>
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        private async void Scrape(object state)
        {
            var stopwatch = Stopwatch.StartNew();
            StopTimer();

            try
            {
                var lolTask = _lolParser.DownloadChattyLolCounts(
                    _previousLols.Json, _previousLols.LolCounts);

                var newChatty = await GetChattyWithoutBodies();

                List<int> threadIdsWithMissingPostBodies;
                if (_chattyProvider.Chatty == null)
                    threadIdsWithMissingPostBodies = newChatty.ThreadsByRootId.Keys.ToList();
                else
                    CopyPostBodies(_chattyProvider.Chatty, newChatty, out threadIdsWithMissingPostBodies);

                await DownloadPostBodies(newChatty, threadIdsWithMissingPostBodies);

                var (lolJson, lolCounts) = _previousLols = await lolTask;

                _chattyProvider.Update(newChatty, lolCounts);
                await _eventProvider.Update(newChatty, lolCounts);

                _logger.LogInformation($"Scrape complete in {stopwatch.Elapsed}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Scrape failed in {stopwatch.Elapsed}. {ex.Message}");
            }
            finally
            {
                StartTimer(runImmediately: false);
            }
        }

        private async Task<Chatty> GetChattyWithoutBodies()
        {
            var chatty =
                new Chatty
                {
                    Threads = new List<ChattyThread>(200),
                    ThreadsByRootId = new Dictionary<int, ChattyThread>(200),
                    ThreadsByReplyId = new Dictionary<int, ChattyThread>(2000),
                    PostsById = new Dictionary<int, ChattyPost>(2000)
                };

            var currentPage = 1;
            var lastPage = 1;

            while (currentPage <= lastPage)
            {
                var (html, chattyPage) =
                    _previousPages.TryGetValue(currentPage, out var prev)
                    ? await _chattyParser.GetChattyPage(currentPage, prev.Html, prev.Page)
                    : await _chattyParser.GetChattyPage(currentPage);
                _previousPages[currentPage] = (html, chattyPage);
                lastPage = chattyPage.LastPage;

                foreach (var thread in chattyPage.Threads)
                {
                    var threadId = thread.Posts[0].Id;

                    // there is a race condition where the same thread could appear on both pages 1 and 2 because we
                    // download them at slightly different times
                    if (chatty.ThreadsByReplyId.ContainsKey(threadId))
                        continue;

                    foreach (var post in thread.Posts)
                    {
                        chatty.ThreadsByReplyId.Add(post.Id, thread);
                        chatty.PostsById.Add(post.Id, post);
                    }

                    chatty.ThreadsByRootId.Add(threadId, thread);
                    chatty.Threads.Add(thread);
                }

                currentPage++;
            }

            return chatty;
        }

        private void CopyPostBodies(Chatty oldChatty, Chatty newChatty,
            out List<int> threadIdsWithMissingPostBodies)
        {
            threadIdsWithMissingPostBodies = new List<int>();

            foreach (var newThread in newChatty.Threads)
            {
                var anyMissingBodies = false;

                foreach (var newPost in newThread.Posts)
                {
                    if (oldChatty.PostsById.TryGetValue(newPost.Id, out var oldPost))
                    {
                        newPost.Body = oldPost.Body;
                        newPost.Date = oldPost.Date;
                    }
                    else
                    {
                        anyMissingBodies = true;
                    }
                }

                if (anyMissingBodies)
                    threadIdsWithMissingPostBodies.Add(newThread.Posts[0].Id);
            }
        }

        private async Task DownloadPostBodies(Chatty newChatty, List<int> threadIdsWithMissingPostBodies)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(
                    threadIdsWithMissingPostBodies,
                    new ParallelOptions { MaxDegreeOfParallelism = 4 },
                    threadId =>
                    {
                        var postBodies = _threadParser.GetThreadBodies(threadId).GetAwaiter().GetResult();

                        foreach (var postBody in postBodies)
                        {
                            if (newChatty.PostsById.TryGetValue(postBody.Id, out var newChattyPost))
                            {
                                newChattyPost.Body = postBody.Body;
                                newChattyPost.Date = postBody.Date;
                            }
                        }

                        // if a post was nuked in between downloading the chatty and downloading the bodies, we might have missed
                        // a body here. let's paper over it
                        if (newChatty.ThreadsByRootId.TryGetValue(threadId, out var newChattyThread))
                            foreach (var newChattyPost in newChattyThread.Posts)
                                if (newChattyPost.Body == null)
                                    newChattyPost.Body = "";
                    });
            });
        }
    }
}
