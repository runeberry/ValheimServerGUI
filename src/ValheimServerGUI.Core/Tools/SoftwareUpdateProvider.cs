using System;
using System.Threading.Tasks;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.Tools
{
    public interface ISoftwareUpdateProvider
    {
        event EventHandler UpdateCheckStarted;

        event EventHandler<SoftwareUpdateEventArgs> UpdateCheckFinished;

        /// <summary>The result of the most recent completed check, or null if none has finished yet. Lets a
        /// window created after the startup check (which runs before the main window exists) show its result.</summary>
        SoftwareUpdateEventArgs? LastResult { get; }

        Task CheckForUpdatesAsync(bool isManualCheck);
    }

    public class SoftwareUpdateEventArgs
    {
        public string? LatestVersion { get; }

        /// <summary>The latest release's page, or null when there is none to link to (no release qualifies, or
        /// the check failed).</summary>
        public string? ReleaseUrl { get; }

        public bool IsManualCheck { get; }

        public bool IsSuccessful { get; }

        public Exception? Exception { get; }

        public SoftwareUpdateEventArgs(
            string latestVersion,
            string? releaseUrl,
            bool isManualCheck)
        {
            LatestVersion = latestVersion;
            ReleaseUrl = releaseUrl;
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
        private readonly IRuneberryApiClient ApiClient;
        private readonly IUserPreferencesProvider UserPrefsProvider;

        private readonly TimeSpan UpdateCheckInterval = CoreConstants.UpdateCheckInterval;
        private DateTime NextAutomaticUpdateCheck = DateTime.MinValue;

        public SoftwareUpdateProvider(IRuneberryApiClient apiClient, IUserPreferencesProvider userPrefsProvider)
        {
            ApiClient = apiClient;
            UserPrefsProvider = userPrefsProvider;
        }

        public event EventHandler? UpdateCheckStarted;

        public event EventHandler<SoftwareUpdateEventArgs>? UpdateCheckFinished;

        public SoftwareUpdateEventArgs? LastResult { get; private set; }

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
                var release = await ApiClient.GetLatestReleaseAsync();

                // No qualifying release: treat the running version as the latest (nothing newer to offer).
                eventArgs = release == null
                    ? new SoftwareUpdateEventArgs(AssemblyHelper.GetApplicationVersion(), null, isManualCheck)
                    : new SoftwareUpdateEventArgs(release.Version!, release.Url, isManualCheck);
            }
            catch (Exception e)
            {
                eventArgs = new SoftwareUpdateEventArgs(e, isManualCheck);
            }

            LastResult = eventArgs;
            UpdateCheckFinished?.Invoke(this, eventArgs);
        }
    }
}
