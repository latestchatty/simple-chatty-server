using System;
using System.Globalization;
using System.Threading.Tasks;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Services
{
    public class CortexParser
    {
        private readonly DownloadService _downloadService;
        private readonly UserParser _userParser;

        public CortexParser(DownloadService downloadService, UserParser userParser)
        {
            _downloadService = downloadService;
            _userParser = userParser;
        }

        public async Task<CortexUserData> GetCortexUserData(string userName)
        {
            var userId = await _userParser.GetUserIdFromName(userName);
            return await GetCortexUserData(userId);
        }

        public async Task<CortexUserData> GetCortexUserData(int userId)
        {
            var page = await _downloadService.DownloadWithSharedLogin($"https://www.shacknews.com/cortex/user/{userId}");
            var parser = new Parser(page);
            var userData = new CortexUserData();
            userData.UserId = userId;
            userData.Username = parser.Clip(new string[] { "<h1 class=\"user-name\"", ">" }, "</h1>");
            //Order matters, because we're lazy here.
            userData.Points = int.Parse(parser.Clip(new string[] { "<span class=\"stat-number\"", ">" }, "</span>"), NumberStyles.Any);
            userData.Comments = int.Parse(parser.Clip(new string[] { "<span class=\"stat-number\"", ">" }, "</span>"), NumberStyles.Any);
            userData.CortexPosts = int.Parse(parser.Clip(new string[] { "<span class=\"stat-number\"", ">" }, "</span>"), NumberStyles.Any);
            userData.Wins = int.Parse(parser.Clip(new string[] { "<span class=\"stat-number\"", ">" }, "</span>"), NumberStyles.Any);
            userData.RegistrationDate = DateTime.Parse(parser.Clip(new string[] { "<span class=\"stat-number\"", ">" }, "</span>"));
            return userData;
        }
    }
}