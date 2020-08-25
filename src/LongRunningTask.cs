using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleChattyServer
{
    public static class LongRunningTask
    {
        public static Task Run(Action action) =>
            Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
    }
}
