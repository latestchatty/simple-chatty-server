using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using SimpleChattyServer.Data;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Services
{
    public sealed class MessageParser
    {
        private readonly DownloadService _downloadService;

        public MessageParser(DownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public async Task<MessagePage> GetMessagePage(Mailbox folder, string username, string password, int page)
        {
            if (page < 1)
                throw new Api400Exception("Invalid page.");

            var folderName = folder == Mailbox.Inbox ? "inbox" : "sent";

            var messagesPerPage = 50;
            var html = await _downloadService.DownloadWithUserLogin(
                $"https://www.shacknews.com/messages/{folderName}?page={page}",
                username, password);

            var p = new Parser(html);

            var messagePage =
                new MessagePage
                {
                    Messages = new List<MessageModel>()
                };
            
            p.Seek(1, "class=\"tools\"");
            messagePage.UnreadCount = int.Parse(p.Clip(new string[] {"<span class=\"flag\"", ">"}, "</span>"));

            p.Seek(1, "<h2>Message Center</h2>");

            if (p.Peek(1, "<div class=\"showing-column\">") == -1)
            {
                messagePage.TotalCount = 0;
                messagePage.LastPage = 1;
            }
            else
            {
                messagePage.TotalCount = int.Parse(p.Clip(
                    new[] { "<div class=\"showing-column\">", ">", "of", " " },
                    "</div>"));
                messagePage.LastPage = (int)Math.Ceiling((double)messagePage.TotalCount / messagesPerPage);
                p.Seek(1, "<ul id=\"messages\">");
            }

            while (p.Peek(1, "<li class=\"message") != -1)
            {
                var message = new MessageModel();
                var liClasses = p.Clip(
                    new[] { "<li class=\"message", "\"" },
                    "\"");
                if (!liClasses.Contains("read"))
                    message.Unread = true;

                message.Id = int.Parse(p.Clip(
                    new[] { "<input type=\"checkbox\" class=\"mid\" name=\"messages[]\"", "value=", "\"" },
                    "\">"));

                var otherUser = WebUtility.HtmlDecode(p.Clip(
                    new[] { "<span class=\"message-username\"", ">" },
                    "</span>"));
                if (folder == Mailbox.Inbox)
                {
                    message.From = otherUser;
                    message.To = username;
                }
                else
                {
                    message.From = username;
                    message.To = otherUser;
                }

                message.Subject = WebUtility.HtmlDecode(p.Clip(
                    new[] { "<span class=\"message-subject\"", ">" },
                    "</span>"));
                message.Date = DateParser.Parse(p.Clip(
                    new[] { "<span class=\"message-date\"", ">" },
                    "</span>"));
                message.Body = p.Clip(
                    new[] { "<div class=\"message-body\"", ">" },
                    "</div>");

                messagePage.Messages.Add(message);
            }

            return messagePage;
        }

        public async Task SendMessage(string username, string password, string recipient, string subject, string body)
        {
            body = body.Replace("\r", "");

            var query = _downloadService.NewQuery();
            query.Add("uid", $"{await GetUserId(username, password)}");
            query.Add("to", recipient);
            query.Add("subject", subject);
            query.Add("message", body);

            await _downloadService.DownloadWithUserLogin(
                "https://www.shacknews.com/messages/send",
                username, password, query.ToString());
        }

        private async Task<int> GetUserId(string username, string password)
        {
            var html = await _downloadService.DownloadWithUserLogin(
                "https://www.shacknews.com/messages", username, password);
            var p = new Parser(html);
            return int.Parse(p.Clip(
                new[] { "<input type=\"hidden\" name=\"uid\"", "value=", "\"" },
                "\">"));
        }

        public async Task DeleteMessage(string username, string password, int id)
        {
            // try both mailboxes
            foreach (var mailbox in new[] { "inbox", "sent" })
                await DeleteMessageInFolder(username, password, id, mailbox);
        }

        public async Task DeleteMessageInFolder(string username, string password, int id, string mailbox)
        {
            var query = _downloadService.NewQuery();
            query.Add("mid", $"{id}");
            query.Add("type", mailbox);

            await _downloadService.DownloadWithUserLogin(
                "https://www.shacknews.com/messages/delete",
                username, password, query.ToString());
        }

        public async Task MarkMessageAsRead(string username, string password, int id)
        {
            var query = _downloadService.NewQuery();
            query.Add("mid", $"{id}");

            await _downloadService.DownloadWithUserLogin(
                "https://www.shacknews.com/messages/read",
                username, password, query.ToString());
        }
    }
}
