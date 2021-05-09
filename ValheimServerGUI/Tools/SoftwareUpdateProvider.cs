using System;
using System.Threading.Tasks;

namespace ValheimServerGUI.Tools
{
    public interface ISoftwareUpdateProvider
    {
        event EventHandler UpdateCheckStarted;

        event EventHandler<SoftwareUpdateEventArgs> UpdateCheckFinished;

        Task CheckForUpdatesAsync(bool isManualCheck);
    }

    public class SoftwareUpdateEventArgs
    {
        public string LatestVersion { get; }

        public bool IsNewerVersionAvailable { get; }

        public bool IsManualCheck { get; }

        public SoftwareUpdateEventArgs(
            string latestVersion, 
            bool isNewerVersionAvailable,
            bool isManualCheck)
        {
            this.LatestVersion = latestVersion;
            this.IsNewerVersionAvailable = isNewerVersionAvailable;
            this.IsManualCheck = isManualCheck;
        }
    }

    public class SoftwareUpdateProvider : ISoftwareUpdateProvider
    {
        private readonly IGitHubClient GitHubClient;

        public SoftwareUpdateProvider(IGitHubClient gitHubClient)
        {
            this.GitHubClient = gitHubClient;
        }

        public event EventHandler UpdateCheckStarted;

        public event EventHandler<SoftwareUpdateEventArgs> UpdateCheckFinished;

        public async Task CheckForUpdatesAsync(bool isManualCheck)
        {
            this.UpdateCheckStarted?.Invoke(this, EventArgs.Empty);

            var currentVersion = AssemblyHelper.GetApplicationVersion();
            var release = await this.GitHubClient.GetLatestReleaseAsync();
            var newerVersionAvailable = AssemblyHelper.IsNewerVersion(release?.TagName);

            // In case there was no response from GitHub, consider the current running version as the "latest version"
            var latestVersion = release?.TagName ?? AssemblyHelper.GetApplicationVersion();

            var eventArgs = new SoftwareUpdateEventArgs(latestVersion, newerVersionAvailable, isManualCheck);
            this.UpdateCheckFinished?.Invoke(this, eventArgs);
        }
    }
}
