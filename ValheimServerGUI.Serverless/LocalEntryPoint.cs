using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ValheimServerGUI.Serverless
{
    /// <summary>
    /// The Main function can be used to run the ASP.NET Core application locally using the Kestrel webserver.
    /// </summary>
    public class LocalEntryPoint
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration(config =>
                    {
                        var executingLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        config.AddJsonFile(System.IO.Path.Join(executingLocation, "appsettings.secret.json"), optional: true);
                        config.AddJsonFile(System.IO.Path.Join(executingLocation, "appsettings.local.json"), optional: true);
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
