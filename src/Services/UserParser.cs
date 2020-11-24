using System.Linq;
using System.Threading.Tasks;

namespace SimpleChattyServer.Services
{
	public sealed class UserParser
	{
		private readonly DownloadService _downloadService;
		private readonly SearchParser _searchParser;
		private readonly ThreadParser _threadParser;

		public UserParser(DownloadService downloadService, SearchParser searchParser, ThreadParser threadParser)
		{
			_downloadService = downloadService;
			_searchParser = searchParser;
			_threadParser = threadParser;
		}

		public async Task<int> GetUserIdFromName(string userName)
		{
			var results = await _searchParser.Search(string.Empty, userName, string.Empty, string.Empty, 0);
			if(results.Results.Count > 0)
			{
				var threadId = results.Results[0].Id;
				var thread = await _threadParser.GetThread(threadId);
				var userPost = thread.Posts.FirstOrDefault(p => p.Author.Equals(userName, System.StringComparison.OrdinalIgnoreCase));
				if (userPost != null) return userPost.AuthorId;
			}
			throw new System.Exception($"Cannot find user id for {userName}");
		}
	}
}