using System;
using System.Collections.Generic;
using System.IO;

namespace ValheimServerGUI.Tools
{
    /// <summary>
    /// Reads and writes a simple configuration file for this application.
    /// </summary>
    /// <remarks>
    /// The config file format is simply lines of: KEY=VALUE
    /// </remarks>
    public class UserPrefs
    {
        public event EventHandler<string> ValueChanged;

        public static readonly UserPrefs Default;

        private static readonly string UserPrefsFilePath = @"%USERPROFILE%\AppData\LocalLow\Runeberry\ValheimServerGUI\userprefs.txt";
        private readonly Dictionary<string, string> ConfigValues = new Dictionary<string, string>();

        static UserPrefs()
        {
            UserPrefsFilePath = Environment.ExpandEnvironmentVariables(UserPrefsFilePath);

            Default = new UserPrefs();
            Default.SetValue(UserPrefsKeys.ValheimGamePath, @"%ProgramFiles(x86)%\Steam\steamapps\common\Valheim");
            Default.SetValue(UserPrefsKeys.ValheimServerPath, @"%ProgramFiles(x86)%\Steam\steamapps\common\Valheim dedicated server\valheim_server.exe");
            Default.SetValue(UserPrefsKeys.ValheimWorldsFolder, @"%USERPROFILE%\AppData\LocalLow\IronGate\Valheim\worlds");
        }

        public UserPrefs()
        {
            this.LoadFile();
        }

        #region Manage values

        /// <summary>
        /// Gets the config value, or returns the provided default value
        /// if the config value is not defined.
        /// </summary>
        public string GetValue(string key, string defaultValue = null)
        {
            return this.ConfigValues.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Gets the config value converted to an integer. Returns the provided default value 
        /// if the config value is not set, or if it cannot be converted to an integer.
        /// </summary>
        public int GetNumberValue(string key, int defaultValue = 0)
        {
            var value = this.GetValue(key);
            return value != null && int.TryParse(value, out var parsed) ? parsed : defaultValue;
        }

        /// <summary>
        /// Gets the config value converted to a boolean flag. Returns the provided default value 
        /// if the config value is not set, or if it cannot be converted to a boolean.
        /// </summary>
        public bool GetFlagValue(string key, bool defaultValue = false)
        {
            var value = this.GetValue(key);
            return value != null && bool.TryParse(value, out var parsed) ? parsed : defaultValue;
        }

        /// <summary>
        /// Gets the config value with any environment variables substituted in (like %APPDATA%).
        /// </summary>
        public string GetEnvironmentValue(string key)
        {
            var value = this.GetValue(key);
            return value != null ? Environment.ExpandEnvironmentVariables(value) : value;
        }

        /// <summary>
        /// Sets the config key to a string representation of the provided value.
        /// If saveNow is true, then the user prefs will be saved to a file immediately.
        /// </summary>
        public void SetValue(string key, object value, bool saveNow = false)
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

            if (saveNow)
            {
                this.SaveFile();
            }

            this.ValueChanged?.Invoke(this, key);
        }

        public void RestoreDefaults(bool saveNow = false)
        {
            // Set all key values to their defaults
            foreach (var (key, value) in Default.ConfigValues)
            {
                this.SetValue(key, value);
            }
            
            // Remove any key values that are not part of the defaults
            foreach (var (key, _) in this.ConfigValues)
            {
                if (!Default.ConfigValues.ContainsKey(key))
                {
                    this.SetValue(key, null);
                }
            }
            
            if (saveNow)
            {
                this.SaveFile();
            }
        }

        #endregion

        #region File IO

        public void LoadFile()
        {
            this.ConfigValues.Clear();

            if (!File.Exists(UserPrefsFilePath))
            {
                this.RestoreDefaults();
            }
            else
            {
                foreach (var line in File.ReadAllLines(UserPrefsFilePath))
                {
                    var parts = line.Split("=");
                    if (parts.Length != 2) continue; // Ignore invalid lines

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    this.ConfigValues[key] = value;
                }
            }
        }

        public void SaveFile()
        {
            if (!File.Exists(UserPrefsFilePath))
            {
                // Recursively create the directories leading up to the file
                var fileInfo = new FileInfo(UserPrefsFilePath);
                fileInfo.Directory.Create();
            }

            var lines = new List<string>();

            foreach (var (key, value) in ConfigValues)
            {
                if (string.IsNullOrWhiteSpace(key)) continue; // Don't save bad keys
                if (value == null) continue; // Don't save null values

                lines.Add($"{key}={value}");
            }

            File.WriteAllLines(UserPrefsFilePath, lines);
        }

        #endregion
    }
}
