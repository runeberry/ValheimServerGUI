using System;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.Tests.Tools
{
    public class MockUserPreferencesProvider : IUserPreferencesProvider
    {
        private UserPreferences UserPrefs = UserPreferences.GetDefault();

        public void SetUserPreferences(UserPreferences prefs)
        {
            this.UserPrefs = prefs;
        }

        public void SetUserPreferences(Action<UserPreferences> prefsBuilder)
        {
            prefsBuilder(this.UserPrefs);
        }

        #region IUserPreferencesProvider implementation

        public event EventHandler<UserPreferences> PreferencesSaved;

        public UserPreferences LoadPreferences()
        {
            return this.UserPrefs;
        }

        public void SavePreferences(UserPreferences preferences)
        {
            this.PreferencesSaved?.Invoke(this, preferences);
        }

        #endregion
    }
}
