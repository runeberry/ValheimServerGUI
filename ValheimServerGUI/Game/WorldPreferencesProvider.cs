using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ValheimServerGUI.Game
{
    public interface IWorldPreferencesProvider
    {
        event EventHandler<List<WorldPreferences>> PreferencesSaved;

        WorldPreferences LoadPreferences(string worldName);

        IEnumerable<WorldPreferences> LoadPreferences();

        void SavePreferences(WorldPreferences preferences);

        void RemovePreferences(string worldName);
    }

    public class WorldPreferencesProvider : IWorldPreferencesProvider
    {
        private readonly IUserPreferencesProvider UserPreferencesProvider;
        private readonly ILogger Logger;

        public WorldPreferencesProvider(IUserPreferencesProvider userPreferencesProvider, ILogger logger)
        {
            UserPreferencesProvider = userPreferencesProvider;
            Logger = logger;

            UserPreferencesProvider.PreferencesSaved += OnPreferencesSaved;
        }

        #region IWorldPreferencesProvider implementation

        public event EventHandler<List<WorldPreferences>> PreferencesSaved;

        public WorldPreferences LoadPreferences(string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName)) throw new ArgumentException($"{nameof(worldName)} must not be null or whitespace");

            var prefs = LoadPreferences().Where(p => p.WorldName == worldName).ToList();

            if (prefs.Count == 0)
            {
                return null;
            }
            else if (prefs.Count > 1)
            {
                Logger.Warning("Multiple configurations found for world '{worldName}'. Returning the most recently updated one.", worldName);
                return prefs.OrderByDescending(p => p.LastSaved).First();
            }

            return prefs.Single();
        }

        public IEnumerable<WorldPreferences> LoadPreferences()
        {
            return UserPreferencesProvider.LoadPreferences().Worlds;
        }

        public void SavePreferences(WorldPreferences preferences)
        {
            if (preferences == null) return;

            var userPrefs = UserPreferencesProvider.LoadPreferences();

            // Remove any existing world preferences with this name
            userPrefs.Worlds.RemoveAll(p => p.WorldName == preferences.WorldName);
            userPrefs.Worlds.Add(preferences);

            preferences.LastSaved = DateTime.UtcNow;

            UserPreferencesProvider.SavePreferences(userPrefs);
            Logger.Information("Saved preferences for world: {worldName}", preferences.WorldName);
        }

        public void RemovePreferences(string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName)) return;

            var userPrefs = UserPreferencesProvider.LoadPreferences();

            if (userPrefs.Worlds.Any(p => p.WorldName == worldName))
            {
                // Remove any existing world preferences with this name
                userPrefs.Worlds.RemoveAll(p => p.WorldName == worldName);

                UserPreferencesProvider.SavePreferences(userPrefs);
                Logger.Information("Removed preferences for world: {worldName}", worldName);
            }
        }

        #endregion

        #region Non-public methods

        private void OnPreferencesSaved(object sender, UserPreferences preferences)
        {
            PreferencesSaved?.Invoke(this, preferences.Worlds);
        }

        #endregion
    }
}
