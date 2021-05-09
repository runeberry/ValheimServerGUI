using System.Threading.Tasks;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools.Http;

namespace ValheimServerGUI.Tools
{
    public interface IRuneberryApiClient
    {
        Task<bool> SendCrashReportAsync(CrashReport report);
    }

    public class RuneberryApiClient : RestClient, IRuneberryApiClient
    {
        public RuneberryApiClient(IRestClientContext context) : base(context)
        {
        }

        public async Task<bool> SendCrashReportAsync(CrashReport report)
        {
            var response = await this.Post($"{Resources.UrlRuneberryApi}/crash-report", report)
                .WithHeader(Secrets.RuneberryApiKeyHeader, Secrets.RuneberryClientApiKey)
                .SendAsync();

            return response.IsSuccessStatusCode;
        }
    }
}
