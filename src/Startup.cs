using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
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
            services.AddSingleton<UserParser>();
            services.AddSingleton<CortexParser>();
            services.AddHostedService<DukeNukedService>();
            services.AddHostedService<ScrapeService>();
            services.AddControllers(
                options => options.Filters.Add(new HttpResponseExceptionFilter()))
                .AddJsonOptions(opts => opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
            services.AddSwaggerGen();
            services.ConfigureSwaggerGen(x =>
            {
                x.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "WinChatty API",
                    Description = "This API implements a subset of versions 1 and 2 of the WinChatty API, allowing it to support preexisting clients of that API.",
                    Contact = new OpenApiContact
                    {
                        Name = "electroly",
                        Email = string.Empty,
                        Url = new Uri("https://github.com/electroly"),
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT",
                        Url = new Uri("https://github.com/latestchatty/simple-chatty-server/blob/master/LICENSE"),
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "WinChatty API");
            });
            
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
