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
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var lolTask = _lolParser.DownloadChattyLolCounts(
                    _state?.LolJson, _state?.LolCounts);

                var (newPages, newChatty) = await GetChattyWithoutBodies(_state?.Pages);

                CopyPostBodies(newChatty);

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

                await SaveState();

                _logger.LogInformation($"Scrape complete in {stopwatch.Elapsed}. Last event is #{await _eventProvider.GetLastEventId()}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Scrape failed in {stopwatch.Elapsed}. {ex.Message}");
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

        private async Task DownloadPostBodies(Chatty newChatty, List<int> threadIdsWithMissingPostBodies)
        {
            await ParallelAsync.ForEach(threadIdsWithMissingPostBodies, 4, async threadId =>
            {
                foreach (var postBody in await _threadParser.GetThreadBodies(threadId))
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
        }
    }
}
