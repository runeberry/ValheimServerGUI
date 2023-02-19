using System;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.Tests.Tools
{
    public class MockUserPreferencesProvider : IUserPreferencesProvider
    {
        private UserPreferences UserPrefs = UserPreferences.GetDefault();

        public void SetUserPreferences(UserPreferences prefs)
        {
            UserPrefs = prefs;
        }

        public void SetUserPreferences(Action<UserPreferences> prefsBuilder)
        {
            prefsBuilder(UserPrefs);
        }

        #region IUserPreferencesProvider implementation

        public event EventHandler<UserPreferences> PreferencesSaved;

        public UserPreferences LoadPreferences()
        {
            return UserPrefs;
        }

        public void SavePreferences(UserPreferences preferences)
        {
            PreferencesSaved?.Invoke(this, preferences);
        }

        #endregion
    }
}
