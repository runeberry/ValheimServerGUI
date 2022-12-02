using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ValheimServerGUI.Game
{
    public interface IServerPreferencesProvider
    {
        event EventHandler<List<ServerPreferences>> PreferencesSaved;

        ServerPreferences LoadPreferences(string serverName);

        IEnumerable<ServerPreferences> LoadPreferences();

        void SavePreferences(ServerPreferences preferences);

        void SavePreferences(IEnumerable<ServerPreferences> preferences);
    }

    public class ServerPreferencesProvider : IServerPreferencesProvider
    {
        private readonly IUserPreferencesProvider UserPreferencesProvider;
        private readonly ILogger Logger;

        public ServerPreferencesProvider(IUserPreferencesProvider userPreferencesProvider, ILogger logger)
        {
            this.UserPreferencesProvider = userPreferencesProvider;
            this.Logger = logger;

            this.UserPreferencesProvider.PreferencesSaved += this.OnPreferencesSaved;
        }

        #region IServerPreferencesProvider implementation

        public event EventHandler<List<ServerPreferences>> PreferencesSaved;

        public ServerPreferences LoadPreferences(string serverName)
        {
            if (string.IsNullOrWhiteSpace(serverName)) throw new ArgumentException($"{nameof(serverName)} must not be null or whitespace");

            var prefs = this.LoadPreferences().Where(p => p.Name == serverName).ToList();

            if (prefs.Count == 0)
            {
                return null;
            }
            else if (prefs.Count > 1)
            {
                this.Logger.LogWarning($"Multiple configurations found for server '{serverName}'. Returning the most recently updated one.");
                return prefs.Last();
            }

            return prefs.Single();
        }

        public IEnumerable<ServerPreferences> LoadPreferences()
        {
            return this.UserPreferencesProvider.LoadPreferences().Servers;
        }

        public void SavePreferences(ServerPreferences preferences)
        {
            if (preferences == null) return;

            var userPrefs = this.UserPreferencesProvider.LoadPreferences();

            // Remove any existing server configurations with this name
            userPrefs.Servers.RemoveAll(p => p.Name == preferences.Name);
            userPrefs.Servers.Add(preferences);

            this.UserPreferencesProvider.SavePreferences(userPrefs);
        }

        public void SavePreferences(IEnumerable<ServerPreferences> preferences)
        {
            var userPrefs = this.UserPreferencesProvider.LoadPreferences();

            userPrefs.Servers = preferences?.ToList() ?? new();

            this.UserPreferencesProvider.SavePreferences(userPrefs);
        }

        #endregion

        #region Non-public methods

        private void OnPreferencesSaved(object sender, UserPreferences preferences)
        {
            this.PreferencesSaved?.Invoke(this, preferences.Servers);
        }

        #endregion
    }
}
