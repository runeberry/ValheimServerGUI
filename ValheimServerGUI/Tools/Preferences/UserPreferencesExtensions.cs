using System;
using System.Collections.Generic;
using System.IO;

namespace ValheimServerGUI.Tools.Preferences
{
    public static class UserPreferencesExtensions
    {
        private static readonly string UserPrefsFilePath = @"%USERPROFILE%\AppData\LocalLow\Runeberry\ValheimServerGUI\userprefs.txt";

        static UserPreferencesExtensions()
        {
            UserPrefsFilePath = Environment.ExpandEnvironmentVariables(UserPrefsFilePath);
        }

        /// <summary>
        /// Gets the config value, or returns the provided default value
        /// if the config value is not defined.
        /// </summary>
        public static string GetValue(this IUserPreferences prefs, string key, string defaultValue)
        {
            return prefs.GetValue(key) ?? defaultValue;
        }

        /// <summary>
        /// Gets the config value converted to an integer. Returns the provided default value 
        /// if the config value is not set, or if it cannot be converted to an integer.
        /// </summary>
        public static int GetNumberValue(this IUserPreferences prefs, string key, int defaultValue = 0)
        {
            var value = prefs.GetValue(key);
            return value != null && int.TryParse(value, out var parsed) ? parsed : defaultValue;
        }

        /// <summary>
        /// Gets the config value converted to a boolean flag. Returns the provided default value 
        /// if the config value is not set, or if it cannot be converted to a boolean.
        /// </summary>
        public static bool GetFlagValue(this IUserPreferences prefs, string key, bool defaultValue = false)
        {
            var value = prefs.GetValue(key);
            return value != null && bool.TryParse(value, out var parsed) ? parsed : defaultValue;
        }

        /// <summary>
        /// Gets the config value with any environment variables substituted in (like %APPDATA%).
        /// </summary>
        public static string GetEnvironmentValue(this IUserPreferences prefs, string key)
        {
            var value = prefs.GetValue(key);
            return value != null ? Environment.ExpandEnvironmentVariables(value) : value;
        }

        /// <summary>
        /// Sets the config key to the provided value and saves the result to file immediately.
        /// </summary>
        public static void SaveValue(this IUserPreferences prefs, string key, object value)
        {
            prefs.SetValue(key, value);
            prefs.SaveFile();
        }

        public static void RestoreDefaults(this IUserPreferences prefs, bool saveNow = false)
        {
            // Set all key values to their defaults
            foreach (var (key, value) in UserPreferences.Default.GetValues())
            {
                prefs.SetValue(key, value);
            }

            // Remove any key values that are not part of the defaults
            foreach (var (key, _) in prefs.GetValues())
            {
                if (UserPreferences.Default.GetValue(key) == null)
                {
                    prefs.SetValue(key, null);
                }
            }

            if (saveNow)
            {
                prefs.SaveFile();
            }
        }

        #region File management

        public static void LoadFile(this IUserPreferences prefs)
        {
            foreach (var (key, _) in prefs.GetValues())
            {
                prefs.SetValue(key, null);
            }

            if (!File.Exists(UserPrefsFilePath))
            {
                prefs.RestoreDefaults();
            }
            else
            {
                foreach (var line in File.ReadAllLines(UserPrefsFilePath))
                {
                    var parts = line.Split("=");
                    if (parts.Length != 2) continue; // Ignore invalid lines

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    prefs.SetValue(key, value);
                }
            }
        }

        public static void SaveFile(this IUserPreferences prefs)
        {
            if (!File.Exists(UserPrefsFilePath))
            {
                // Recursively create the directories leading up to the file
                var fileInfo = new FileInfo(UserPrefsFilePath);
                fileInfo.Directory.Create();
            }

            var lines = new List<string>();

            foreach (var (key, value) in prefs.GetValues())
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
