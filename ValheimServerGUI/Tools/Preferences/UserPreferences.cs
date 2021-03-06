using System.Collections.Generic;
using ValheimServerGUI.Properties;

namespace ValheimServerGUI.Tools.Preferences
{
    /// <summary>
    /// Reads and writes a simple configuration file for this application.
    /// </summary>
    /// <remarks>
    /// The config file format is simply lines of: KEY=VALUE
    /// </remarks>
    public class UserPreferences : IUserPreferences
    {
        public static readonly IUserPreferences Default;

        private readonly Dictionary<string, string> ConfigValues = new Dictionary<string, string>();

        static UserPreferences()
        {
            Default = new UserPreferences();
            Default.SetValue(PrefKeys.ValheimGamePath, Resources.DefaultGamePath);
            Default.SetValue(PrefKeys.ValheimServerPath, Resources.DefaultServerPath);
            Default.SetValue(PrefKeys.ValheimWorldsFolder, Resources.DefaultWorldsFolder);
            Default.SetValue(PrefKeys.ServerPort, Resources.DefaultServerPort);
        }

        #region IUserPreferencesProvider implementation

        /// <inheritdoc/>
        public event KeyValueEventHandler ValueChanged;

        /// <inheritdoc/>
        public IEnumerable<(string, string)> GetValues()
        {
            foreach (var (key, value) in this.ConfigValues)
            {
                yield return (key, value);
            }
        }

        /// <inheritdoc/>
        public string GetValue(string key)
        {
            return this.ConfigValues.TryGetValue(key, out var value) ? value : null;
        }

        /// <inheritdoc/>
        public void SetValue(string key, object value)
        {
            var strValue = value?.ToString();
            var existingValue = this.GetValue(key);

            if (strValue == existingValue) return; // Nothing to change

            if (string.IsNullOrWhiteSpace(strValue))
            {
                // Remove null/empty values, don't leave them hanging in the dictionary
                this.ConfigValues.Remove(strValue);
            }
            else
            {
                this.ConfigValues[key] = strValue;
            }

            this.ValueChanged?.Invoke(this, key, strValue);
        }

        #endregion
    }
}
