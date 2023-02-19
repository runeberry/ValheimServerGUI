using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ValheimServerGUI.Game
{
    public interface IServerPreferencesProvider
    {
        event EventHandler<List<ServerPreferences>> PreferencesSaved;

        ServerPreferences LoadPreferences(string profileName);

        IEnumerable<ServerPreferences> LoadPreferences();

        void SavePreferences(ServerPreferences preferences);

        void RemovePreferences(string profileName);
    }

    public class ServerPreferencesProvider : IServerPreferencesProvider
    {
        private readonly IUserPreferencesProvider UserPreferencesProvider;
        private readonly ILogger Logger;

        public ServerPreferencesProvider(IUserPreferencesProvider userPreferencesProvider, ILogger logger)
        {
            UserPreferencesProvider = userPreferencesProvider;
            Logger = logger;

            UserPreferencesProvider.PreferencesSaved += OnPreferencesSaved;
        }

        #region IServerPreferencesProvider implementation

        public event EventHandler<List<ServerPreferences>> PreferencesSaved;

        public ServerPreferences LoadPreferences(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) throw new ArgumentException($"{nameof(profileName)} must not be null or whitespace");

            var prefs = LoadPreferences().Where(p => p.ProfileName == profileName).ToList();

            if (prefs.Count == 0)
            {
                return null;
            }
            else if (prefs.Count > 1)
            {
                Logger.LogWarning($"Multiple configurations found for server '{profileName}'. Returning the most recently updated one.");
                return prefs.OrderByDescending(p => p.LastSaved).First();
            }

            return prefs.Single();
        }

        public IEnumerable<ServerPreferences> LoadPreferences()
        {
            return UserPreferencesProvider.LoadPreferences().Servers;
        }

        public void SavePreferences(ServerPreferences preferences)
        {
            if (preferences == null) return;

            var userPrefs = UserPreferencesProvider.LoadPreferences();

            // Remove any existing server profiles with this name
            userPrefs.Servers.RemoveAll(p => p.ProfileName == preferences.ProfileName);
            userPrefs.Servers.Add(preferences);

            preferences.LastSaved = DateTime.UtcNow;

            UserPreferencesProvider.SavePreferences(userPrefs);
            Logger.LogInformation("Saved preferences for server profile: {profileName}", preferences.ProfileName);
        }

        public void RemovePreferences(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return;

            var userPrefs = UserPreferencesProvider.LoadPreferences();

            if (userPrefs.Servers.Any(p => p.ProfileName == profileName))
            {
                // Remove any existing server profiles with this name
                userPrefs.Servers.RemoveAll(p => p.ProfileName == profileName);

                UserPreferencesProvider.SavePreferences(userPrefs);
                Logger.LogInformation("Removed preferences for server profile: {profileName}", profileName);
            }
        }

        #endregion

        #region Non-public methods

        private void OnPreferencesSaved(object sender, UserPreferences preferences)
        {
            PreferencesSaved?.Invoke(this, preferences.Servers);
        }

        #endregion
    }
}
