using System;
using System.Diagnostics;
using System.Threading;

namespace SimpleChattyServer
{
    public sealed class LoggedReaderWriterLockSlim
    {
        private const double MIN_LOG_MSEC = 10;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly string _name;
        private readonly Action<string> _logAction;

        public LoggedReaderWriterLockSlim(string name, Action<string> logAction)
        {
            _name = name;
            _logAction = logAction;
        }

        public void WithReadLock(string caller, Action action)
        {
            var sw = Stopwatch.StartNew();
            _lock.EnterReadLock();
            try
            {
                if (sw.Elapsed.TotalMilliseconds > MIN_LOG_MSEC)
                    _logAction?.Invoke($"Caller \"{caller}\" entered read lock \"{_name}\" in {sw.Elapsed.TotalMilliseconds:0} ms.");
                sw.Restart();
                action();
            }
            finally
            {
                _lock.ExitReadLock();
                if (sw.Elapsed.TotalMilliseconds > MIN_LOG_MSEC)
                    _logAction?.Invoke($"Caller \"{caller}\" held read lock \"{_name}\" for {sw.Elapsed.TotalMilliseconds:0} ms.");
            }
        }

        public void WithWriteLock(string caller, Action action)
        {
            var sw = Stopwatch.StartNew();
            _lock.EnterWriteLock();
            try
            {
                if (sw.Elapsed.TotalMilliseconds > MIN_LOG_MSEC)
                    _logAction?.Invoke($"Caller \"{caller}\" entered write lock \"{_name}\" in {sw.Elapsed.TotalMilliseconds:0} ms.");
                sw.Restart();
                action();
            }
            finally
            {
                _lock.ExitWriteLock();
                if (sw.Elapsed.TotalMilliseconds > MIN_LOG_MSEC)
                    _logAction?.Invoke($"Caller \"{caller}\" held write lock \"{_name}\" for {sw.Elapsed.TotalMilliseconds:0} ms.");
            }
        }

        public T WithReadLock<T>(string caller, Func<T> func)
        {
            var value = default(T);
            WithReadLock(caller, action: () => value = func());
            return value;
        }

        public T WithWriteLock<T>(string caller, Func<T> func)
        {
            var value = default(T);
            WithWriteLock(caller, action: () => value = func());
            return value;
        }
    }
}
