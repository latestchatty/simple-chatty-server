using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SimpleChattyServer.Data;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Services
{
    public sealed class ThreadParser
    {
        private static readonly Regex _stripTagsRegex = new Regex("<[^>]*(>|$)", RegexOptions.Compiled);
        private readonly DownloadService _downloadService;

        public ThreadParser(DownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public void CheckContentId(string html)
        {
            var contentTypeIdPrefix = "<input type=\"hidden\" name=\"content_type_id\" id=\"content_type_id\" value=\"";
            var contentTypeIdPos = html.IndexOf(contentTypeIdPrefix);
            if (contentTypeIdPos == -1)
                return;

            var i = contentTypeIdPos + contentTypeIdPrefix.Length;
            var contentTypeIdStr = new StringBuilder();
            while (html[i] != '"')
            {
                contentTypeIdStr.Append(html[i]);
                i++;
            }

            var contentTypeId = int.Parse(contentTypeIdStr.ToString());
            if (contentTypeId != 2 && contentTypeId != 17)
                throw new MissingThreadException($"This post is not in the main chatty. Content type ID: {contentTypeId}");
        }

        public async Task<ChattyThread> GetThread(int id)
        {
            // id might be a reply ID instead of a root thread ID. GetThreadTree() can handle it.
            var tree = await GetThreadTree(id);

            // From tree we can grab the real root thread ID.
            var threadId = tree.Posts[0].Id;

            // Add the bodies to the tree.
            var bodies = (await GetThreadBodies(threadId)).ToDictionary(x => x.Id);
            foreach (var treePost in tree.Posts)
            {
                if (bodies.TryGetValue(treePost.Id, out var bodyPost))
                {
                    treePost.Body = bodyPost.Body;
                    treePost.Date = bodyPost.Date;
                }
            }

            return tree;
        }

        // if the ID doesn't exist, the returned list will be empty
        public async Task<List<ChattyPost>> GetThreadBodies(int threadId)
        {
            var url = $"https://www.shacknews.com/frame_laryn.x?root={threadId}";
            var html = await _downloadService.DownloadWithSharedLogin(url, verifyLoginStatus: false);

            if (!html.Contains("</html>"))
                throw new ParsingException("Shacknews thread tree HTML ended prematurely.");

            var p = new Parser(html);
            var list = new List<ChattyPost>();

            while (p.Peek(1, "<div id=\"item_") != -1)
            {
                var reply = new ChattyPost();
                reply.Id = int.Parse(p.Clip(
                    new[] { "<div id=\"item_", "_" },
                    "\">"));
                reply.Category = V2ModerationFlagConverter.Parse(p.Clip(
                    new[] { "<div class=\"fullpost", "fpmod_", "_" },
                    " "));
                reply.Author = HtmlDecodeExceptLtGt(p.Clip(
                    new[] { "<span class=\"author\">", "<span class=\"user\">", "<a rel=\"nofollow\" href=\"/user/", ">" },
                    "</a>")).Trim();
                reply.Body = MakeSpoilersClickable(HtmlDecodeExceptLtGt(p.Clip(
                    new[] { "<div class=\"postbody\">", ">" },
                    "</div>"))).Trim();
                reply.Date = DateParser.Parse(StripTags(p.Clip(
                    new[] { "<div class=\"postdate\">", ">" },
                    "T</div")) + "T");
                list.Add(reply);
            }

            return list;
        }

        public async Task<ChattyThread> GetThreadTree(int id)
        {
            var url = $"https://www.shacknews.com/chatty?id={id}";
            var html = await _downloadService.DownloadWithSharedLogin(url);

            if (!html.Contains("</html>"))
                throw new ParsingException("Shacknews thread tree HTML ended prematurely.");

            if (html.Contains("<p class=\"be_first_to_comment\">"))
                throw new MissingThreadException("This post is in the future.");

            CheckContentId(html);

            var p = new Parser(html);
            p.Seek(1, "<div class=\"threads\">");

            return ParseThreadTree(p, stopAtFullPost: false);
        }

        public ChattyThread ParseThreadTree(Parser p, bool stopAtFullPost = true)
        {
            if (p.Peek(1, "<div class=\"postbody\">") == -1)
                throw new MissingThreadException($"Thread does not exist.");

            var list = new List<ChattyPost>();
            var rootBody = MakeSpoilersClickable(HtmlDecodeExceptLtGt(p.Clip(
                new[] { "<div class=\"postbody\">", ">" },
                "</div>"))).Trim();
            var rootDate = DateParser.Parse(StripTags(p.Clip(
                new[] { "<div class=\"postdate\">", ">" },
                "T</div")) + "T");

            var depth = 0;
            var nextThread = p.Peek(1, "<div class=\"fullpost op");
            if (nextThread == -1)
                nextThread = p.Length;

            while (true)
            {
                var nextReply = p.Peek(1, "<div class=\"oneline");
                if (nextReply == -1 || (stopAtFullPost && nextReply > nextThread))
                    break;

                var reply = new ChattyPost { Depth = depth };

                if (list.Count == 0)
                {
                    reply.Body = rootBody;
                    reply.Date = rootDate;
                }

                reply.Category = V2ModerationFlagConverter.Parse(p.Clip(
                    new[] { "<div class=\"oneline", "olmod_", "_" },
                    " "));
                reply.Id = int.Parse(p.Clip(
                    new[] { "<a class=\"shackmsg\" rel=\"nofollow\" href=\"?id=", "id=", "=" },
                    "\""));
                reply.Author = HtmlDecodeExceptLtGt(p.Clip(
                    new[] { "<span class=\"oneline_user", ">" },
                    "</span>"));

                // Determine the next level of depth.
                while (true)
                {
                    var nextLi = p.Peek(1, "<li ");
                    var nextUl = p.Peek(1, "<ul>");
                    var nextEndUl = p.Peek(1, "</ul>");

                    if (nextLi == -1)
                        nextLi = nextThread;
                    if (nextUl == -1)
                        nextUl = nextThread;
                    if (nextEndUl == -1)
                        nextEndUl = nextThread;

                    var next = Math.Min(Math.Min(nextLi, nextUl), nextEndUl);

                    if (next == nextThread)
                    {
                        // This thread has no more replies.
                        break;
                    }
                    else if (next == nextLi)
                    {
                        // Next reply is on the same depth level.
                        break;
                    }
                    else if (next == nextUl)
                    {
                        // Next reply is underneath this one.
                        depth++;
                    }
                    else if (next == nextEndUl)
                    {
                        // Next reply is above this one.
                        depth--;
                    }

                    p.Cursors[1] = next + 1;
                }

                list.Add(reply);
            }

            return new ChattyThread { Posts = list };
        }

        public async Task<(int ContentTypeId, int ContentId)> GetContentTypeId(int postId)
        {
            var html = await _downloadService.DownloadWithSharedLogin($"https://www.shacknews.com/chatty?id={postId}");
            
            var p = new Parser(html);
            p.Seek(1, "<input type=\"hidden\" name=\"content_type_id\"");
            var contentTypeId = int.Parse(p.Clip(
                new[] { "value=\"", "\"" },
                "\""));
            var contentId = int.Parse(p.Clip(
                new[] { "value=\"", "\"" },
                "\""));
            return (contentTypeId, contentId);
        }

        public async Task<bool> DoesThreadExist(int threadId)
        {
            try
            {
                await GetThreadTree(threadId);
                return true;
            }
            catch (MissingThreadException)
            {
                return false;
            }
        }

        public static string PreviewFromBody(string body) =>
            HtmlDecodeExceptLtGt(
                CollapseWhitespace(
                    StripTags(
                        RemoveSpoilers(body)
                        .Replace("<br />", " ")
                        .Replace("<br/>", " ")
                        .Replace("<br>", " "))));

        private static string RemoveSpoilers(string text)
        {
            var spoilerSpan = "span class=\"jt_spoiler\"";

            var spoilerSpanLen = spoilerSpan.Length;
            var span = "span ";
            var spanLen = span.Length;
            var endSpan = "/span>";
            var endSpanLen = endSpan.Length;
            var replaceStr = "_______";
            var output = new StringBuilder();
            var inSpoiler = false;
            var depth = 0;

            // Split by < to get all the tags separated out.
            var textParts = text.Split('<');
            for (var i = 0; i < textParts.Length; i++)
            {
                var chunk = textParts[i];
                if (i == 0)
                {
                    // The first chunk does not start with or contain a <, so we can
                    // just copy it directly to the output.
                    output.Append(chunk);
                }
                else if (inSpoiler)
                {
                    if (chunk.Length >= spanLen && chunk.Substring(0, spanLen) == span)
                    {
                        // Nested Shacktag.
                        depth++;
                    }
                    else if (chunk.Length >= endSpanLen && chunk.Substring(0, endSpanLen) == endSpan)
                    {
                        // End of a Shacktag.
                        depth--;

                        // If the depth has dropped back to zero, then we found the end
                        // of the spoilered text.
                        if (depth == 0)
                        {
                            output.Append(chunk.Substring(endSpanLen));
                            inSpoiler = false;
                        }
                    }
                }
                else
                {
                    if (chunk.Length >= spoilerSpanLen && chunk.Substring(0, spoilerSpanLen) == spoilerSpan)
                    {
                        // Beginning of a spoiler.
                        inSpoiler = true;
                        depth = 1;
                        output.Append(replaceStr);
                    }
                    else
                    {
                        output.Append("<");
                        output.Append(chunk);
                    }
                }
            }

            return output.ToString();
        }

        private static string MakeSpoilersClickable(string text) =>
            text.Replace("return doSpoiler(event);", "this.className = '';");

        private static string StripTags(string html) =>
            _stripTagsRegex.Replace(html, "");

        private static string StrReplaceAll(string needle, string replacement, string haystack)
        {
            while (haystack.Contains(needle))
                haystack = haystack.Replace(needle, replacement);

            return haystack;
        }

        private static string CollapseWhitespace(string str)
        {
            str =
                str
                .Replace("\n", "")
                .Replace("\t", "")
                .Replace("\r", "");

            str = StrReplaceAll("  ", " ", str);

            return str.Trim();
        }

        private static string HtmlDecodeExceptLtGt(string str) =>
            WebUtility.HtmlDecode(
                str
                .Replace("&lt;", "&amp;lt;")
                .Replace("&gt;", "&amp;gt;"));
    }
}
