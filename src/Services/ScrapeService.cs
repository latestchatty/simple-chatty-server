using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleChattyServer.Data;
using SimpleChattyServer.Data.Options;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Services
{
    public sealed class ScrapeService : IHostedService, IDisposable
    {
        private const string STATE_FILENAME = "scrape-state.json.gz";

        private readonly ILogger _logger;
        private readonly ChattyParser _chattyParser;
        private readonly ThreadParser _threadParser;
        private readonly LolParser _lolParser;
        private readonly DownloadService _downloadService;
        private readonly ChattyProvider _chattyProvider;
        private readonly EventProvider _eventProvider;
        private readonly StorageOptions _storageOptions;
        private readonly Timer _timer;
        private ScrapeState _state;

        public ScrapeService(ILogger<ScrapeService> logger, ChattyParser chattyParser, ThreadParser threadParser,
            LolParser lolParser, DownloadService downloadService, ChattyProvider chattyProvider,
            EventProvider eventProvider, IOptions<StorageOptions> storageOptions)
        {
            _logger = logger;
            _chattyParser = chattyParser;
            _threadParser = threadParser;
            _lolParser = lolParser;
            _downloadService = downloadService;
            _chattyProvider = chattyProvider;
            _eventProvider = eventProvider;
            _storageOptions = storageOptions.Value;
            _timer = new Timer(Scrape, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await LoadState();
            StartTimer(0);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            StopTimer();
            await SaveState();
        }

        private void StartTimer(double seconds) =>
            _timer.Change(TimeSpan.FromSeconds(seconds), Timeout.InfiniteTimeSpan);

        private void StopTimer() =>
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        private async Task LoadState()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                using var fileStream = File.OpenRead(GetStateFilePath());
                using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                _state = await JsonSerializer.DeserializeAsync<ScrapeState>(gzipStream);
                _state.Chatty.SetDictionaries();
                await _eventProvider.PrePopulate(_state.Chatty, _state.LolCounts, _state.Events);
                _logger.LogInformation($"Loaded state in {sw.Elapsed}. Last event is #{await _eventProvider.GetLastEventId()}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read state; using new state.");
            }
        }

        private async Task SaveState()
        {
            try
            {
                if (_state != null)
                {
                    var sw = Stopwatch.StartNew();
                    _state.Events = await _eventProvider.CloneEventsList();
                    var filePath = GetStateFilePath();
                    if (File.Exists(filePath))
                        File.Move(filePath, filePath + ".bak", overwrite: true);
                    using var fileStream = File.Create(filePath);
                    using var gzipStream = new GZipStream(fileStream, CompressionLevel.Fastest);
                    await JsonSerializer.SerializeAsync(gzipStream, _state);
                    _logger.LogInformation($"Saved state in {sw.Elapsed}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save state.");
            }
        }

        private string GetStateFilePath() =>
            Path.Combine(_storageOptions.DataPath, STATE_FILENAME);

        private async void Scrape(object state)
        {
            var stopwatch = Stopwatch.StartNew();
            StopTimer();

            try
            {
                var lolTask = _lolParser.DownloadChattyLolCounts(
                    _state?.LolJson, _state?.LolCounts);

                var (newPages, newChatty) = await GetChattyWithoutBodies(_state?.Pages);

                await CopyPostBodies(newChatty);

                var threadIdsWithMissingPostBodies = new List<int>();
                foreach (var thread in newChatty.Threads)
                    if (thread.Posts.Any(x => x.Body == null))
                        threadIdsWithMissingPostBodies.Add(thread.ThreadId);

                await DownloadPostBodies(newChatty, threadIdsWithMissingPostBodies);

                var (lolJson, lolCounts) = await lolTask;

                _chattyProvider.Update(newChatty, lolCounts);
                await _eventProvider.Update(newChatty, lolCounts);

                _state =
                    new ScrapeState
                    {
                        Chatty = newChatty,
                        Pages = newPages,
                        LolJson = lolJson,
                        LolCounts = lolCounts
                    };

                _logger.LogInformation($"Scrape complete in {stopwatch.Elapsed}. Last event is #{await _eventProvider.GetLastEventId()}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Scrape failed in {stopwatch.Elapsed}. {ex.Message}");
            }
            finally
            {
                var seconds = 5 - stopwatch.Elapsed.TotalSeconds;
                StartTimer(seconds < 0 ? 5 : seconds);
            }
        }

        private async Task<(List<ScrapeState.Page> Pages, Chatty Chatty)> GetChattyWithoutBodies(
            List<ScrapeState.Page> previousPages)
        {
            var chatty = new Chatty { Threads = new List<ChattyThread>(200) };

            var currentPage = 1;
            var lastPage = 1;
            var newPages = new List<ScrapeState.Page>();
            var seenThreadIds = new HashSet<int>();

            while (currentPage <= lastPage)
            {
                var previousPage =
                    previousPages != null && previousPages.Count >= currentPage
                    ? previousPages[currentPage - 1]
                    : null;
                var (html, chattyPage) =
                    previousPage != null
                    ? await _chattyParser.GetChattyPage(currentPage, previousPage.Html, previousPage.ChattyPage)
                    : await _chattyParser.GetChattyPage(currentPage);
                newPages.Add(new ScrapeState.Page { Html = html, ChattyPage = chattyPage });
                lastPage = chattyPage.LastPage;

                foreach (var thread in chattyPage.Threads)
                {
                    var threadId = thread.Posts[0].Id;

                    // there is a race condition where the same thread could appear on both pages 1 and 2 because we
                    // download them at slightly different times
                    if (seenThreadIds.Contains(threadId))
                    {
                        for (var i = 0; i < chatty.Threads.Count; i++)
                            if (chatty.Threads[i].ThreadId == threadId)
                                chatty.Threads[i] = thread;
                    }
                    else
                    {
                        chatty.Threads.Add(thread);
                        seenThreadIds.Add(thread.ThreadId);
                    }
                }

                // the other race condition is that a thread can be missed because it moved from page 2 to 1 in
                // between our requests of pages 1 and 2. let's add the old thread here, and if it turns out that we
                // see it on page 2, then we'll overwrite this old version
                if (previousPage != null)
                {
                    foreach (var previousThread in previousPage.ChattyPage.Threads)
                    {
                        if (!seenThreadIds.Contains(previousThread.ThreadId))
                        {
                            chatty.Threads.Add(previousThread);
                            seenThreadIds.Add(previousThread.ThreadId);
                        }
                    }
                }

                currentPage++;
            }

            chatty.SetDictionaries();
            return (newPages, chatty);
        }

        private async Task CopyPostBodies(Chatty newChatty)
        {
            if (_state == null)
                return;

            await Task.Run(() =>
            {
                var oldPostsById = (
                    from page in _state.Pages
                    from thread in page.ChattyPage.Threads
                    from post in thread.Posts
                    select post
                    ).ToDictionary(x => x.Id);

                foreach (var newThread in newChatty.Threads)
                {
                    foreach (var newPost in newThread.Posts)
                    {
                        if (oldPostsById.TryGetValue(newPost.Id, out var oldPost))
                        {
                            newPost.Body = oldPost.Body;
                            newPost.Date = oldPost.Date;
                        }
                    }
                }
            });
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
                    });

                // bail if there are still missing bodies. this happens in rare situations where shacknews itself fails
                // to include a new post in the bodies response
                foreach (var thread in newChatty.Threads)
                    foreach (var post in thread.Posts)
                        if (post.Body == null)
                            throw new ParsingException($"Missing body for post {post.Id}.");
            });
        }
    }
}
