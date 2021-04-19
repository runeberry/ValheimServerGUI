using System;
using System.Security;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ValheimServerGUI.Tools
{
    /// <summary>
    /// It attempts to write to HKEY_LOCAL_MACHINE first, which will run on startup on all user accounts.
    /// If it fails (due to lack of privileges), it attempts HKEY_CURRENT_USER, which will only run the program
    /// on the current Windows account the user is logged into.
    /// </summary>
    /// <remarks>
    /// Adapted from: https://gist.github.com/HelBorn/2266242
    /// </remarks>
    public class StartupHelper
    {
        private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static bool ApplyStartupSetting(bool userPreference, ILogger logger)
        {
            var startupPath = GetStartupPath(Application.ProductName, logger);
            var isCurrentlyEnabled = startupPath != null;

            if (userPreference)
            {
                var runOnStartup = false;

                if (!isCurrentlyEnabled)
                {
                    runOnStartup = true;
                }
                else if (!startupPath.Equals(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("ValheimServerGUI executable path has changed, removing old registry entry...");
                    if (RemoveFromStartup(Application.ProductName, logger))
                    {
                        runOnStartup = true;
                    }
                }

                if (runOnStartup && RunOnStartup(Application.ProductName, Application.ExecutablePath, logger))
                {
                    logger.LogInformation("ValheimServerGUI will now run on Windows startup");
                    return true;
                }
            }
            else if (!userPreference && isCurrentlyEnabled)
            {
                if (RemoveFromStartup(Application.ProductName, logger))
                {
                    logger.LogInformation("ValheimServerGUI will no longer run on Windows startup");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds the specified executable to the startup list.
        /// </summary>
        /// <param name="appTitle">Registry key title.</param>
        /// <param name="appPath">Path of executable to run on startup.</param>
        private static bool RunOnStartup(string appTitle, string appPath, ILogger logger)
        {
            RegistryKey rk;
            try
            {
                rk = Registry.LocalMachine.OpenSubKey(RegistryKey, true);
                rk.SetValue(appTitle, appPath);
                return true;
            }
            catch (SecurityException e)
            {
                logger.LogWarning($"{nameof(RunOnStartup)}: No LocalMachine access (not Administrator), trying CurrentUser...");
            }
            catch (Exception e)
            {
                logger.LogException(e, $"{nameof(RunOnStartup)}: Failed to set LocalMachine registry key, trying CurrentUser...");
            }

            try
            {
                rk = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                rk.SetValue(appTitle, appPath);
            }
            catch (Exception e)
            {
                logger.LogException(e, $"{nameof(RunOnStartup)}: Failed to set CurrentUser registry key");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Removes the specified executable from the startup list.
        /// </summary>
        /// <param name="appTitle">Registry key title.</param>
        /// <param name="appPath">Path of executable in the registry that's being run on startup.</param>
        private static bool RemoveFromStartup(string appTitle, ILogger logger)
        {
            RegistryKey rk;
            try
            {
                rk = Registry.LocalMachine.OpenSubKey(RegistryKey, true);
                rk.DeleteValue(appTitle);
                return true;
            }
            catch (SecurityException e)
            {
                logger.LogWarning($"{nameof(RemoveFromStartup)}: No LocalMachine access (not Administrator), trying CurrentUser...");
            }
            catch (Exception e)
            {
                logger.LogException(e, $"{nameof(RemoveFromStartup)}: Failed to set LocalMachine registry key, trying CurrentUser...");
            }

            try
            {
                rk = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                rk.DeleteValue(appTitle);
                return true;
            }
            catch (Exception e)
            {
                logger.LogException(e, $"{nameof(RemoveFromStartup)}: Failed to set CurrentUser registry key");
            }

            return false;
        }

        /// <summary>
        /// Checks if the executable is in the startup list with the specified path.
        /// </summary>
        /// <param name="appTitle">Registry key title.</param>
        /// <param name="appPath">Path of the executable.</param>
        private static string GetStartupPath(string appTitle, ILogger logger)
        {
            RegistryKey rk;

            try
            {
                rk = Registry.LocalMachine.OpenSubKey(RegistryKey, true);
                return rk.GetValue(appTitle)?.ToString();
            }
            catch (SecurityException e)
            {
                logger.LogWarning($"{nameof(GetStartupPath)}: No LocalMachine access (not Administrator), trying CurrentUser...");
            }
            catch (Exception e)
            {
                logger.LogException(e, $"{nameof(GetStartupPath)}: Failed to read LocalMachine registry key, trying CurrentUser...");
            }

            try
            {
                rk = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                return rk.GetValue(appTitle)?.ToString();
            }
            catch (Exception e)
            {
                logger.LogException(e, $"{nameof(GetStartupPath)}: Failed to read CurrentUser registry key");
            }

            return null;
        }
    }
}
