using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SimpleChattyServer.Services
{
    public sealed class UserParser
    {
        private const double CACHE_TTL_HOURS = 24;
        private readonly DownloadService _downloadService;
        private readonly SearchParser _searchParser;
        private readonly ThreadParser _threadParser;
        private readonly ILogger<UserParser> _logger;
        private readonly Dictionary<string, int> userNameToIdMapCache = new Dictionary<string, int>();
        private readonly LoggedReaderWriterLock _userIdCacheLock;

        private DateTime _cacheExpire = DateTime.UtcNow.AddHours(CACHE_TTL_HOURS);

        public UserParser(ILogger<UserParser> logger, DownloadService downloadService, SearchParser searchParser, ThreadParser threadParser)
        {
            _downloadService = downloadService;
            _searchParser = searchParser;
            _threadParser = threadParser;
            _logger = logger;
            _userIdCacheLock = new LoggedReaderWriterLock(nameof(_userIdCacheLock), x => _logger.LogDebug(x));
        }

        public async Task<int> GetUserIdFromName(string userName)
        {
            await _userIdCacheLock.WithWriteLock(nameof(GetUserIdFromName) + userName, () =>
            {
                if (_cacheExpire < DateTime.UtcNow)
                {
                    _cacheExpire = DateTime.UtcNow.AddHours(CACHE_TTL_HOURS);
                    userNameToIdMapCache.Clear();
                }
            });

            var id = await _userIdCacheLock.WithReadLock(nameof(GetUserIdFromName) + userName, () =>
                {
                    if (userNameToIdMapCache.TryGetValue(userName, out var cachedId))
                        return (int?)cachedId;
                    else
                        return default(int?);
                });

            if (id.HasValue) return id.Value;

            var results = await _searchParser.Search(string.Empty, userName, string.Empty, string.Empty, 0);
            if (results.Results.Count > 0)
            {
                var threadId = results.Results[0].Id;
                var thread = await _threadParser.GetThread(threadId);
                var userPost = thread.Posts.FirstOrDefault(p => p.Author.Equals(userName, System.StringComparison.OrdinalIgnoreCase));
                if (userPost != null)
                {
                    await _userIdCacheLock.WithWriteLock(nameof(GetUserIdFromName) + userName, () =>
                        {
                            userNameToIdMapCache[userName] = userPost.AuthorId;
                        });

                    return userPost.AuthorId;
                }
            }

            throw new System.Exception($"Cannot find user id for {userName}");
        }
    }
}