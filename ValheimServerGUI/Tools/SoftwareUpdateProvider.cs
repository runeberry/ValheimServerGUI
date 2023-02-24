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

        public bool IsManualCheck { get; }

        public bool IsSuccessful { get; }

        public Exception Exception { get; }

        public SoftwareUpdateEventArgs(
            string latestVersion,
            bool isManualCheck)
        {
            LatestVersion = latestVersion;
            IsManualCheck = isManualCheck;
            IsSuccessful = true;
        }

        public SoftwareUpdateEventArgs(
            Exception e,
            bool isManualCheck)
        {
            Exception = e;
            IsManualCheck = isManualCheck;
            IsSuccessful = false;
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
            GitHubClient = gitHubClient;
            UserPrefsProvider = userPrefsProvider;
        }

        public event EventHandler UpdateCheckStarted;

        public event EventHandler<SoftwareUpdateEventArgs> UpdateCheckFinished;

        public async Task CheckForUpdatesAsync(bool isManualCheck)
        {
            if (!isManualCheck)
            {
                // Only fulfill automated checks if enough time has passed since the last check
                var now = DateTime.UtcNow;
                if (now < NextAutomaticUpdateCheck) return;
                NextAutomaticUpdateCheck = now + UpdateCheckInterval;

                // Only fulfill automated checks if the user has update checks enabled
                var prefs = UserPrefsProvider.LoadPreferences();
                if (!prefs.CheckForUpdates) return;
            }

            UpdateCheckStarted?.Invoke(this, EventArgs.Empty);

            SoftwareUpdateEventArgs eventArgs;

            try
            {
                var currentVersion = AssemblyHelper.GetApplicationVersion();
                var release = await GitHubClient.GetLatestReleaseAsync();

                // In case there was no response from GitHub, consider the current running version as the "latest version"
                var latestVersion = release?.TagName ?? AssemblyHelper.GetApplicationVersion();

                eventArgs = new SoftwareUpdateEventArgs(latestVersion, isManualCheck);
            }
            catch (Exception e)
            {
                eventArgs = new SoftwareUpdateEventArgs(e, isManualCheck);
            }

            UpdateCheckFinished?.Invoke(this, eventArgs);
        }
    }
}
