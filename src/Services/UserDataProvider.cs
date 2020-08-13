using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleChattyServer.Data;
using SimpleChattyServer.Data.Options;

namespace SimpleChattyServer.Services
{
    public sealed class UserDataProvider
    {
        private readonly ILogger _logger;
        private readonly UserDataOptions _userDataOptions;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public UserDataProvider(ILogger<UserDataProvider> logger, IOptions<UserDataOptions> userDataOptions)
        {
            _logger = logger;
            _userDataOptions = userDataOptions.Value;
        }

        public async Task<UserData> GetUserData(string username)
        {
            try
            {
                return await Task.Run(() =>
                {
                    _lock.EnterReadLock();
                    try
                    {
                        var filePath = GetFilePath(username);
                        if (!File.Exists(filePath))
                            return new UserData();

                        using var stream = File.OpenRead(filePath);
                        return JsonSerializer.DeserializeAsync<UserData>(stream).GetAwaiter().GetResult();
                    }
                    finally
                    {
                        _lock.ExitReadLock();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to read user data file for \"{username}\".");
                return new UserData();
            }
        }

        public async Task UpdateUserData(string username, Action<UserData> action)
        {
            await Task.Run(() =>
            {
                _lock.EnterWriteLock();
                try
                {
                    var filePath = GetFilePath(username);

                    UserData userData;
                    if (File.Exists(filePath))
                    {
                        using var stream = File.OpenRead(filePath);
                        userData = JsonSerializer.DeserializeAsync<UserData>(stream).GetAwaiter().GetResult();
                    }
                    else
                    {
                        userData = new UserData();
                    }

                    action(userData);

                    using (var stream = File.Create(filePath))
                        JsonSerializer.SerializeAsync(stream, userData).Wait();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            });
        }

        private string GetFilePath(string username)
        {
            var sb = new StringBuilder();
            sb.Append("userdata.");
            foreach (var ch in username.ToLowerInvariant())
            {
                if (ch >= 'a' && ch <= 'z')
                    sb.Append(ch);
                else if (ch == ' ')
                    sb.Append('_');
                else
                    sb.Append(((int)ch).ToString("X").PadLeft(4, '0'));
            }
            sb.Append(".json");
            var str = sb.ToString();
            if (str.Length > 100)
                str = str.Substring(0, 100);
            return Path.Combine(_userDataOptions.Path, str);
        }
    }
}
