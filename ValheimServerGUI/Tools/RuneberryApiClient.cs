using System.Threading.Tasks;
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
            var response = await this.Post("https://api.runeberry.com/vsg-api/crash-report", report)
                .SendAsync();

            return response.IsSuccessStatusCode;
        }
    }
}
