using System;
using System.IO;
using LettuceEncrypt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleChattyServer.Data.Options;
using SimpleChattyServer.Services;

namespace SimpleChattyServer
{
    public sealed class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var storageSection = Configuration.GetSection(StorageOptions.SectionName);
            var dataPath = storageSection.GetValue<string>("DataPath");
            if (string.IsNullOrWhiteSpace(dataPath) || !Directory.Exists(dataPath))
                throw new Exception("Must configure DataPath.");
            if (Environment.IsProduction())
                services.AddLettuceEncrypt().PersistDataToDirectory(
                    new DirectoryInfo(dataPath), null);
            services.AddResponseCompression(
                options => options.EnableForHttps = true);
            services.AddCors(
                cors => cors.AddDefaultPolicy(
                    builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
            services.Configure<DukeNukedOptions>(Configuration.GetSection(DukeNukedOptions.SectionName));
            services.Configure<SharedLoginOptions>(Configuration.GetSection(SharedLoginOptions.SectionName));
            services.Configure<StorageOptions>(storageSection);
            services.AddSingleton<ChattyProvider>();
            services.AddSingleton<ChattyParser>();
            services.AddSingleton<DownloadService>();
            services.AddSingleton<EmojiConverter>();
            services.AddSingleton<EventProvider>();
            services.AddSingleton<FrontPageParser>();
            services.AddSingleton<LolParser>();
            services.AddSingleton<MessageParser>();
            services.AddSingleton<SearchParser>();
            services.AddSingleton<ThreadParser>();
            services.AddSingleton<UserDataProvider>();
            services.AddHostedService<DukeNukedService>();
            services.AddHostedService<ScrapeService>();
            services.AddControllers(
                options => options.Filters.Add(new HttpResponseExceptionFilter()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();
            app.UseMiddleware<RequestLogMiddleware>();
            app.UseCors();
            app.UseRouting();
            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapDefaultControllerRoute();
                });
        }
    }
}
