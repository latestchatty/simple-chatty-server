using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleChattyServer.Data.Options;
using SimpleChattyServer.Services;

namespace SimpleChattyServer
{
    public sealed class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression();
            services.Configure<SharedLoginOptions>(Configuration.GetSection(SharedLoginOptions.SectionName));
            services.Configure<UserDataOptions>(Configuration.GetSection(UserDataOptions.SectionName));
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
            services.AddHostedService<ScrapeService>();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();
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
