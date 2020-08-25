using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleChattyServer
{
    public static class ParallelAsync
    {
        public static async Task ForEach<T>(IEnumerable<T> list, int maxTasks, Func<T, Task> func)
        {
            var tasks = new List<Task>(maxTasks);
            foreach (var item in list)
            {
                if (tasks.Count >= maxTasks)
                {
                    var completedTask = await Task.WhenAny(tasks);
                    tasks.Remove(completedTask);
                }

                tasks.Add(func(item));
            }

            await Task.WhenAll(tasks);
        }
    }
}
