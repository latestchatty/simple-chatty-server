using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace SimpleChattyServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(128, 128);
            using var host = CreateHostBuilder(args).Build();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, builder) =>
                {
                    builder.AddJsonFile("appsettings.json");
                    builder.AddEnvironmentVariables();
                    builder.AddCommandLine(args);
                })
                .UseSystemd()
                .ConfigureLogging((context, builder) =>
                {
                    builder.ClearProviders();
                    builder.AddConsole(
                        options => options.FormatterName = ConsoleFormatterNames.Systemd);
                })
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}
