using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace SimpleChattyServer
{
    public sealed class LoggedReaderWriterLock
    {
        private const double MIN_LOG_MSEC = 50;
        private readonly AsyncReaderWriterLock _lock = new AsyncReaderWriterLock();
        private readonly string _name;
        private readonly Action<string> _logAction;

        public LoggedReaderWriterLock(string name, Action<string> logAction)
        {
            _name = name;
            _logAction = logAction;
        }

        public async Task WithReadLock(string caller, Func<Task> actionAsync)
        {
            var sw = Stopwatch.StartNew();
            using (await _lock.ReaderLockAsync())
            {
                if (sw.Elapsed.TotalMilliseconds > MIN_LOG_MSEC)
                    _logAction?.Invoke($"Caller \"{caller}\" entered read lock \"{_name}\" in {sw.Elapsed.TotalMilliseconds:##,#0} ms.");
                sw.Restart();
                await actionAsync();
            }
            if (sw.Elapsed.TotalMilliseconds > MIN_LOG_MSEC)
                _logAction?.Invoke($"Caller \"{caller}\" held read lock \"{_name}\" for {sw.Elapsed.TotalMilliseconds:##,#0} ms.");
        }

        public Task WithReadLock(string caller, Action action) =>
            WithReadLock(caller,
                actionAsync: () =>
                {
                    action();
                    return Task.CompletedTask;
                });

        public async Task WithWriteLock(string caller, Func<Task> actionAsync)
        {
            var sw = Stopwatch.StartNew();
            using (await _lock.WriterLockAsync())
            {
                if (sw.Elapsed.TotalMilliseconds > MIN_LOG_MSEC)
                    _logAction?.Invoke($"Caller \"{caller}\" entered write lock \"{_name}\" in {sw.Elapsed.TotalMilliseconds:##,#0} ms.");
                sw.Restart();
                await actionAsync();
            }
            if (sw.Elapsed.TotalMilliseconds > MIN_LOG_MSEC)
                _logAction?.Invoke($"Caller \"{caller}\" held write lock \"{_name}\" for {sw.Elapsed.TotalMilliseconds:##,#0} ms.");
        }

        public Task WithWriteLock(string caller, Action action) =>
            WithWriteLock(caller,
                actionAsync: () =>
                {
                    action();
                    return Task.CompletedTask;
                });

        public async Task<T> WithReadLock<T>(string caller, Func<T> func)
        {
            var value = default(T);
            await WithReadLock(caller, action: () => value = func());
            return value;
        }

        public async Task<T> WithReadLock<T>(string caller, Func<Task<T>> funcAsync)
        {
            var value = default(T);
            await WithReadLock(caller, actionAsync: async () => value = await funcAsync());
            return value;
        }

        public async Task<T> WithWriteLock<T>(string caller, Func<T> func)
        {
            var value = default(T);
            await WithWriteLock(caller, action: () => value = func());
            return value;
        }

        public async Task<T> WithWriteLock<T>(string caller, Func<Task<T>> funcAsync)
        {
            var value = default(T);
            await WithWriteLock(caller, action: async () => value = await funcAsync());
            return value;
        }
    }
}
