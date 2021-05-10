using System;
using System.Threading.Tasks;
using ValheimServerGUI.Game;
using ValheimServerGUI.Properties;

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

        public bool IsSuccessful { get; }

        public Exception Exception { get; }

        public SoftwareUpdateEventArgs(
            string latestVersion, 
            bool isNewerVersionAvailable,
            bool isManualCheck)
        {
            this.LatestVersion = latestVersion;
            this.IsNewerVersionAvailable = isNewerVersionAvailable;
            this.IsManualCheck = isManualCheck;
            this.IsSuccessful = true;
        }

        public SoftwareUpdateEventArgs(
            Exception e, 
            bool isManualCheck)
        {
            this.Exception = e;
            this.IsNewerVersionAvailable = false;
            this.IsManualCheck = isManualCheck;
            this.IsSuccessful = false;
        }
    }

    public class SoftwareUpdateProvider : ISoftwareUpdateProvider
    {
        private readonly IGitHubClient GitHubClient;
        private readonly IUserPreferencesProvider UserPrefsProvider;

        private readonly TimeSpan UpdateCheckInterval = TimeSpan.Parse(Resources.UpdateCheckInterval);
        private DateTime NextAutomaticUpdateCheck = DateTime.MinValue;

        public SoftwareUpdateProvider(IGitHubClient gitHubClient, IUserPreferencesProvider userPrefsProvider)
        {
            this.GitHubClient = gitHubClient;
            this.UserPrefsProvider = userPrefsProvider;
        }

        public event EventHandler UpdateCheckStarted;

        public event EventHandler<SoftwareUpdateEventArgs> UpdateCheckFinished;

        public async Task CheckForUpdatesAsync(bool isManualCheck)
        {
            if (!isManualCheck)
            {
                // Only fulfill automated checks if enough time has passed since the last check
                var now = DateTime.UtcNow;
                if (now < this.NextAutomaticUpdateCheck) return;
                this.NextAutomaticUpdateCheck = now + this.UpdateCheckInterval;

                // Only fulfill automated checks if the user has update checks enabled
                var prefs = this.UserPrefsProvider.LoadPreferences();
                if (!prefs.CheckForUpdates) return;
            }

            this.UpdateCheckStarted?.Invoke(this, EventArgs.Empty);

            SoftwareUpdateEventArgs eventArgs;

            try
            {
                var currentVersion = AssemblyHelper.GetApplicationVersion();
                var release = await this.GitHubClient.GetLatestReleaseAsync();
                var newerVersionAvailable = AssemblyHelper.IsNewerVersion(release?.TagName);

                // In case there was no response from GitHub, consider the current running version as the "latest version"
                var latestVersion = release?.TagName ?? AssemblyHelper.GetApplicationVersion();

                eventArgs = new SoftwareUpdateEventArgs(latestVersion, newerVersionAvailable, isManualCheck);
            }
            catch (Exception e)
            {
                eventArgs = new SoftwareUpdateEventArgs(e, isManualCheck);
            }

            this.UpdateCheckFinished?.Invoke(this, eventArgs);
        }
    }
}
