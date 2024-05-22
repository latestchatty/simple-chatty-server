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
        private ScrapeState _state;
        private Task _task;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

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
        }

        public void Dispose()
        {
            _cts.Dispose();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await LoadState();
            _task = LongRunningTask.Run(() => ScrapeLoop(_cts.Token).GetAwaiter().GetResult());
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping scraper");
            _cts.Cancel();
            if (_task != null)
                await _task;
        }

        private async Task LoadState()
        {
            var sw = Stopwatch.StartNew();

            // in case the main save is corrupted, try the backup save
            foreach (var filePath in new[] { GetStateFilePath(), GetStateFilePath() + ".bak" })
            {
                try
                {
                    using var fileStream = File.OpenRead(filePath);
                    using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                    _state = await JsonSerializer.DeserializeAsync<ScrapeState>(gzipStream);
                    _state.Chatty.SetDictionaries();
                    await _eventProvider.PrePopulate(_state.Chatty, _state.LolCounts, _state.Events);
                    _logger.LogInformation($"Loaded state in {sw.Elapsed}. Last event is #{await _eventProvider.GetLastEventId()}.");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to read state from \"{filePath}\".");
                }
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save state.");
            }
        }

        private string GetStateFilePath() =>
            Path.Combine(_storageOptions.DataPath, STATE_FILENAME);


        private async Task ScrapeLoop(CancellationToken cancel)
        {
            var stopwatch = Stopwatch.StartNew();

            while (!cancel.IsCancellationRequested)
            {
                var nextRun = stopwatch.Elapsed.Add(TimeSpan.FromSeconds(5));
                
                await Scrape();

                var now = stopwatch.Elapsed;
                if (now < nextRun)
                {
                    try
                    {
                        await Task.Delay(nextRun - now, cancel);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private async Task<TimeSpan> Scrape()
        {
            var stopwatch = new StepStopwatch();

            try
            {
                var oldEventId = await _eventProvider.GetLastEventId();

                stopwatch.Step(nameof(GC));
                GC.Collect();

                stopwatch.Step(nameof(_lolParser.DownloadChattyLolCounts));
                var lolTask = _lolParser.DownloadChattyLolCounts(_state?.LolJson, _state?.LolCounts);

                var (newPages, newChatty) = await GetChattyWithoutBodies(_state?.Pages, stopwatch);

                if (_state != null)
                {
                    stopwatch.Step(nameof(HandleThreadsThatDisappeared));
                    await HandleThreadsThatDisappeared(_state.Chatty, newChatty);
                }

                stopwatch.Step(nameof(ReorderThreads));
                ReorderThreads(newChatty);

                stopwatch.Step(nameof(newChatty.SetDictionaries));
                newChatty.SetDictionaries();

                stopwatch.Step(nameof(CopyPostBodies));
                CopyPostBodies(newChatty);

                stopwatch.Step(nameof(DownloadPostBodies));
                await DownloadPostBodies(newChatty);

                stopwatch.Step(nameof(RemovePostsWithNoBody));
                RemovePostsWithNoBody(newChatty);

                stopwatch.Step(nameof(Cortex.DetectCortexThreads));
                Cortex.DetectCortexThreads(newChatty);

                stopwatch.Step(nameof(FixRelativeLinks));
                FixRelativeLinks(newChatty);

                stopwatch.Step(nameof(_chattyProvider.Update));
                var (lolJson, lolCounts) = await lolTask;
                await _eventProvider.Update(newChatty, lolCounts);
                _chattyProvider.Update(newChatty, lolCounts);

                _state =
                    new ScrapeState
                    {
                        Chatty = newChatty,
                        Pages = newPages,
                        LolJson = lolJson,
                        LolCounts = lolCounts
                    };

                var newEventId = await _eventProvider.GetLastEventId();
                if (oldEventId != newEventId)
                {
                    stopwatch.Step(nameof(SaveState));
                    await SaveState();
                }

                ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
                ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
                _logger.LogInformation("Scrape complete. Last event is #{EventId}. {Elapsed}. Worker threads: {WorkerCount}. IOCP threads: {CompletionPortCount}.",
                    await _eventProvider.GetLastEventId(), stopwatch, maxWorkerThreads - availableWorkerThreads,
                    maxCompletionPortThreads - availableCompletionPortThreads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Scrape failed. {stopwatch}. {ex.Message}");
            }

            return stopwatch.Elapsed;
        }

        private async Task<(List<ScrapeState.Page> Pages, Chatty Chatty)> GetChattyWithoutBodies(
            List<ScrapeState.Page> previousPages, StepStopwatch stopwatch)
        {
            ScrapeState.Page GetPreviousPage(int page) =>
                previousPages != null && previousPages.Count >= page
                ? previousPages[page - 1]
                : null;

            var chatty = new Chatty { Threads = new List<ChattyThread>(200) };

            var currentPage = 1;
            var lastPage = 1;
            var newPages = new List<ScrapeState.Page>();
            var seenThreadIds = new HashSet<int>();

            // The chatty almost always has three or fewer pages, so try downloading the first three in parallel for speed.
            stopwatch.Step("First three pages");
            var firstThreePages = new (string Html, ChattyPage Page)[3];
            await Parallel.ForEachAsync(Enumerable.Range(1, 3), async (x, cancel) =>
            {
                var prev = GetPreviousPage(x);
                StepStopwatch pageStopwatch = new();
                var page = await _chattyParser.GetChattyPage(pageStopwatch, x, prev?.Html, prev?.ChattyPage);
                _logger.LogInformation("Scrape page {Page}: {Stopwatch}", x, pageStopwatch);
                firstThreePages[x - 1] = page;
            });

            while (currentPage <= lastPage)
            {
                var previousPage = GetPreviousPage(currentPage);
                var (html, chattyPage) =
                    currentPage <= 3
                    ? firstThreePages[currentPage - 1]
                    : await _chattyParser.GetChattyPage(stopwatch, currentPage, previousPage?.Html, previousPage?.ChattyPage);
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

                currentPage++;
            }

            return (newPages, chatty);
        }

        private async Task HandleThreadsThatDisappeared(Chatty oldChatty, Chatty newChatty)
        {
            var newChattyThreadIds = newChatty.Threads.Select(x => x.ThreadId).ToHashSet();
            foreach (var oldThread in oldChatty.Threads)
            {
                var threadId = oldThread.ThreadId;
                if (!newChattyThreadIds.Contains(threadId))
                {
                    if (await _threadParser.DoesThreadExist(threadId))
                    {
                        if (DateTimeOffset.Now - oldThread.Posts[0].Date > TimeSpan.FromHours(24))
                            newChatty.ExpiredThreadIds.Add(threadId);
                        else
                            newChatty.Threads.Add(oldThread);
                    }
                    else
                    {
                        newChatty.NukedThreadIds.Add(threadId);
                    }
                }
            }
        }

        private void ReorderThreads(Chatty newChatty)
        {
            var lastPostIds = new Dictionary<int, int>(); // thread id => last post id
            foreach (var thread in newChatty.Threads)
                lastPostIds.Add(thread.ThreadId, thread.Posts.Max(x => x.Id));
            newChatty.Threads =
                newChatty.Threads
                .OrderByDescending(x => lastPostIds[x.ThreadId])
                .ToList();
        }

        private void CopyPostBodies(Chatty newChatty)
        {
            if (_state == null)
                return;

            var oldPostsById = _state.Chatty.Threads.SelectMany(x => x.Posts)
                .GroupBy(x => x.Id).Select(x => x.First()) // workaround for thread merging
                .ToDictionary(x => x.Id);

            foreach (var newThread in newChatty.Threads)
            {
                foreach (var newPost in newThread.Posts)
                {
                    if (oldPostsById.TryGetValue(newPost.Id, out var oldPost))
                    {
                        newPost.Body = oldPost.Body;
                        newPost.Date = oldPost.Date;
                        newPost.AuthorId = oldPost.AuthorId;
                        newPost.AuthorFlair = oldPost.AuthorFlair;
                    }
                }
            }
        }

        private async Task DownloadPostBodies(Chatty newChatty)
        {
            var threadIdsWithMissingPostBodies = new List<int>();
            foreach (var thread in newChatty.Threads)
                if (thread.Posts.Any(x => x.Body == null))
                    threadIdsWithMissingPostBodies.Add(thread.ThreadId);

            await Parallel.ForEachAsync(
                threadIdsWithMissingPostBodies,
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                async (threadId, cancel) =>
                {
                    foreach (var postBody in await _threadParser.GetThreadBodies(threadId))
                    {
                        if (newChatty.PostsById.TryGetValue(postBody.Id, out var newChattyPost))
                        {
                            newChattyPost.Body = postBody.Body;
                            newChattyPost.Date = postBody.Date;
                            newChattyPost.AuthorId = postBody.AuthorId;
                            newChattyPost.AuthorFlair = postBody.AuthorFlair;
                            newChattyPost.IsFrozen = postBody.IsFrozen;
                        }
                    }
                });
        }

        private void RemovePostsWithNoBody(Chatty chatty)
        {
            var anyChanges = false;
            foreach (var thread in chatty.Threads)
            {
                for (var postIndex = 0; postIndex < thread.Posts.Count; postIndex++)
                {
                    var subthreadRootPost = thread.Posts[postIndex];
                    if (subthreadRootPost.Body == null)
                    {
                        // remove the subthread rooted at this post
                        anyChanges = true;
                        do
                        {
                            _logger.LogDebug($"Removing post with no body: {thread.Posts[postIndex].Id}");
                            thread.Posts.RemoveAt(postIndex);
                        }
                        while (postIndex < thread.Posts.Count &&
                            thread.Posts[postIndex].Depth > subthreadRootPost.Depth);

                        postIndex--;
                    }
                }
            }
            if (anyChanges)
                chatty.SetDictionaries();
        }

        private void FixRelativeLinks(Chatty newChatty)
        {
            foreach (var x in newChatty.PostsById.Values)
            {
                x.Body = x.Body
                    .Replace("<a href=\"/", "<a href=\"https://www.shacknews.com/")
                    .Replace("<a target=\"_blank\" href=\"/", "<a target=\"_blank\" href=\"https://www.shacknews.com/");
            }
        }
    }
}
