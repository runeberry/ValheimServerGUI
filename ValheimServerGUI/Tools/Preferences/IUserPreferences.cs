using System.Collections.Generic;

namespace ValheimServerGUI.Tools.Preferences
{
    public interface IUserPreferences
    {
        /// <summary>
        /// Invoked whenever a config value is changed. Provides the config key and the changed value.
        /// </summary>
        event KeyValueEventHandler ValueChanged;

        /// <summary>
        /// Gets all config values. Returns key, value pairs.
        /// </summary>
        IEnumerable<(string, string)> GetValues();

        /// <summary>
        /// Gets the config value, or returns null if the config value is not defined.
        /// </summary>
        string GetValue(string key);

        /// <summary>
        /// Sets the config key to a string representation of the provided value.
        /// </summary>
        void SetValue(string key, object value);
    }
}
