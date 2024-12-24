using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleChattyServer.Data;
using SimpleChattyServer.Data.Options;

namespace SimpleChattyServer.Services
{
    public sealed class DukeNukedService : IHostedService, IDisposable
    {
        private const string LAST_MESSAGE_ID_FILENAME = "duke_nuked_last_message_id.txt";

        private static readonly Regex _stripTagsRegex = new Regex("<[^>]*(>|$)", RegexOptions.Compiled);
        private readonly ILogger _logger;
        private readonly MessageParser _messageParser;
        private readonly ChattyProvider _chattyProvider;
        private readonly DownloadService _downloadService;
        private readonly DukeNukedOptions _dukeNukedOptions;
        private readonly StorageOptions _storageOptions;
        private readonly Timer _timer;

        public DukeNukedService(ILogger<DukeNukedService> logger, MessageParser messageParser,
            ChattyProvider chattyProvider, DownloadService downloadService,
            IOptions<DukeNukedOptions> dukeNukedOptions, IOptions<StorageOptions> storageOptions)
        {
            _logger = logger;
            _messageParser = messageParser;
            _chattyProvider = chattyProvider;
            _downloadService = downloadService;
            _dukeNukedOptions = dukeNukedOptions.Value;
            _storageOptions = storageOptions.Value;
            _timer = new Timer(Run, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_dukeNukedOptions.Enabled)
                StartTimer(runImmediately: true);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopTimer();
            return Task.CompletedTask;
        }

        private void StartTimer(bool runImmediately) =>
            _timer.Change(runImmediately ? TimeSpan.Zero : TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        private void StopTimer() =>
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        private async void Run(object state)
        {
            StopTimer();
            try
            {
                var lastMessageId = await ReadLastMessageId();
                var messagePage = await _messageParser.GetMessagePage(
                    Mailbox.Inbox, _dukeNukedOptions.Username, _dukeNukedOptions.Password, 1);
                var newLastMessageId = GetLastMessageId(messagePage);
                foreach (var newMessage in messagePage.Messages
                    .Where(x => x.Id > lastMessageId).OrderBy(x => x.Date))
                {
                    await _messageParser.MarkMessageAsRead(
                        _dukeNukedOptions.Username, _dukeNukedOptions.Password, newMessage.Id);
                    if (!await SendMessageNotification(newMessage)) {
                        break;
                    }
                    await WriteLastMessageId(newMessage.Id);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
                _logger.LogInformation($"DukeNuked complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"DukeNuked failed. {ex.Message}");
            }
            finally
            {
                StartTimer(runImmediately: false);
            }
        }

        private string GetLastMessageIdFilePath() =>
            Path.Combine(_storageOptions.DataPath, LAST_MESSAGE_ID_FILENAME);

        private int GetLastMessageId(MessagePage messagePage) =>
            messagePage.Messages.Any() ? messagePage.Messages.Max(x => x.Id) : 0;

        private async Task<int> ReadLastMessageId()
        {
            var filePath = GetLastMessageIdFilePath();
            try
            {
                return int.Parse(await File.ReadAllTextAsync(filePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read file \"{FilePath}\".", filePath);
            }

            var messagePage = await _messageParser.GetMessagePage(
                Mailbox.Inbox, _dukeNukedOptions.Username, _dukeNukedOptions.Password, 1);
            return GetLastMessageId(messagePage);
        }

        private async Task WriteLastMessageId(int id)
        {
            var filePath = GetLastMessageIdFilePath();
            await File.WriteAllTextAsync(filePath, $"{id}");
        }

        private async Task<bool> SendMessageNotification(MessageModel newMessage)
        {
            var postBody =
                $"From: {newMessage.From}\r\n" +
                $"Date: {newMessage.Date}\r\n" +
                $"Subject: {newMessage.Subject}\r\n\r\n" +
                _stripTagsRegex.Replace(newMessage.Body, "");

            var query = _downloadService.NewQuery();
            query.Add("token", _dukeNukedOptions.SlackToken);
            query.Add("channel", "#duke-nuked");
            query.Add("text", postBody);
            query.Add("link_names", "false");
            query.Add("unfurl_links", "false");
            query.Add("unfurl_media", "false");
            query.Add("mrkdwn", "false");

            var result = await _downloadService.DownloadAnonymous(
                "https://slack.com/api/chat.postMessage", query);

            if (result.Contains("\"ok\":false")) {
                _logger.LogError("Slack API returned error: {Result}", result);
                return false;
            } else {
                _logger.LogInformation("Slack result: {Result}", result);
                return true;
            }
        }
    }
}
