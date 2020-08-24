using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleChattyServer
{
    public static class LongRunningTask
    {
        public static Task<TResult> Run<TResult>(Func<TResult> func) =>
            Task.Factory.StartNew(
                func,
                CancellationToken.None,
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

        public static Task Run(Action action) =>
            Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
    }
}
