using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ValheimServerGUI.Serverless.Middleware;
using ValheimServerGUI.Serverless.Services;
using ValheimServerGUI.Tools.Http;

namespace ValheimServerGUI.Serverless
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static IConfiguration Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            services.AddSingleton<ILogger, ServerlessLogger>();
            services.AddSingleton<IHttpClientProvider, HttpClientProvider>();
            services.AddSingleton<IRestClientContext, RestClientContext>();

            services.AddSingleton<ISteamApiClient, SteamApiClient>();
            services.AddSingleton<IXboxApiClient, XboxApiClient>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.Use(RuneberryAuthMiddleware.Authorize);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Welcome to running ASP.NET Core on AWS Lambda");
                });
            });
        }
    }
}
