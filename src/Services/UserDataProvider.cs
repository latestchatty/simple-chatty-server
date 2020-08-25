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
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Services
{
    public sealed class UserDataProvider
    {
        private readonly ILogger _logger;
        private readonly StorageOptions _storageOptions;
        private readonly LoggedReaderWriterLock _lock;

        public UserDataProvider(ILogger<UserDataProvider> logger, IOptions<StorageOptions> storageOptions)
        {
            _logger = logger;
            _storageOptions = storageOptions.Value;

            _lock = new LoggedReaderWriterLock(
                nameof(UserDataProvider),
                x => _logger.LogDebug(x));
        }

        public async Task<UserData> GetUserData(string username)
        {
            if (string.IsNullOrEmpty(username))
                throw new Api400Exception(Api400Exception.Codes.INVALID_LOGIN, "Username must be provided.");

            try
            {
                return await _lock.WithReadLock(nameof(GetUserData),
                    funcAsync: async () =>
                    {
                        var filePath = GetFilePath(username);
                        if (!File.Exists(filePath))
                            return new UserData();

                        using var stream = File.OpenRead(filePath);
                        return await JsonSerializer.DeserializeAsync<UserData>(stream);
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
            if (string.IsNullOrEmpty(username))
                throw new Api400Exception(Api400Exception.Codes.INVALID_LOGIN, "Username must be provided.");

            await _lock.WithWriteLock(nameof(UpdateUserData),
                actionAsync: async () =>
                {
                    var filePath = GetFilePath(username);

                    UserData userData;
                    if (File.Exists(filePath))
                    {
                        using var stream = File.OpenRead(filePath);
                        userData = await JsonSerializer.DeserializeAsync<UserData>(stream);
                    }
                    else
                    {
                        userData = new UserData();
                    }

                    action(userData);

                    using (var stream = File.Create(filePath))
                        await JsonSerializer.SerializeAsync(stream, userData);
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
            return Path.Combine(_storageOptions.DataPath, str);
        }
    }
}
