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
            while (!cancel.IsCancellationRequested)
            {
                var elapsed = await Scrape();
                var delay = 5 - elapsed.TotalSeconds;
                if (delay < 0)
                    delay = 5;
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delay), cancel);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private async Task<TimeSpan> Scrape()
        {
            var stopwatch = new StepStopwatch();

            try
            {
                var oldEventId = await _eventProvider.GetLastEventId();

                stopwatch.Step(nameof(GetChattyWithoutBodies));
                var lolTask = _lolParser.DownloadChattyLolCounts(_state?.LolJson, _state?.LolCounts);
                var (newPages, newChatty) = await GetChattyWithoutBodies(_state?.Pages);

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

                _logger.LogInformation($"Scrape complete. Last event is #{await _eventProvider.GetLastEventId()}. {stopwatch}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Scrape failed. {stopwatch}. {ex.Message}");
            }

            return stopwatch.Elapsed;
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
                        if (DateTimeOffset.Now - oldThread.Posts[0].Date > TimeSpan.FromHours(18))
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

            var oldPostsById = _state.Chatty.Threads.SelectMany(x => x.Posts).ToDictionary(x => x.Id);

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
        }

        private async Task DownloadPostBodies(Chatty newChatty)
        {
            var threadIdsWithMissingPostBodies = new List<int>();
            foreach (var thread in newChatty.Threads)
                if (thread.Posts.Any(x => x.Body == null))
                    threadIdsWithMissingPostBodies.Add(thread.ThreadId);

            await ParallelAsync.ForEach(threadIdsWithMissingPostBodies, 4, async threadId =>
            {
                foreach (var postBody in await _threadParser.GetThreadBodies(threadId))
                    SetBody(postBody);
            });

            void SetBody(ChattyPost postBody)
            {
                if (newChatty.PostsById.TryGetValue(postBody.Id, out var newChattyPost))
                {
                    newChattyPost.Body = postBody.Body;
                    newChattyPost.Date = postBody.Date;
                }
            }
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
