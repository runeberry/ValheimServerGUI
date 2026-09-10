using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Game
{
    public interface ISteamCloudWorldProvider
    {
        /// <summary>
        /// Names of worlds saved to Steam Cloud (deduped across userdata accounts). Never throws.
        /// </summary>
        IEnumerable<string> GetCloudWorldNames();

        /// <summary>
        /// The Steam Cloud source folder for a world, or null if not found. Never throws.
        /// </summary>
        DirectoryInfo GetCloudWorldFolder(string worldName);

        /// <summary>
        /// Brings a Steam Cloud world into the server's local save folder. Throws on failure.
        /// </summary>
        DirectoryInfo ImportCloudWorld(string worldName, DirectoryInfo destSaveFolder, bool move);
    }

    /// <summary>
    /// Worlds created in-game save to Steam Cloud (userdata\...\892970\remote), which the dedicated
    /// server never reads. This surfaces those worlds and copies/moves them into the local savedir so
    /// they can be hosted. The "remote" folder is itself a drop-in savedir, so world enumeration reuses
    /// <see cref="ValheimPathExtensions.GetWorldNames"/> verbatim.
    /// </summary>
    public class SteamCloudWorldProvider : ISteamCloudWorldProvider
    {
        private readonly ILogger Logger;

        // Steam's install path is stable for the process lifetime, so probe the registry once.
        private bool SteamPathResolved;
        private string SteamPath;

        public SteamCloudWorldProvider(ILogger logger)
        {
            Logger = logger;
        }

        public IEnumerable<string> GetCloudWorldNames()
        {
            try
            {
                return GetRemoteFolders()
                    .SelectMany(remote => remote.GetWorldNames())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public DirectoryInfo GetCloudWorldFolder(string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName)) return null;

            try
            {
                foreach (var remote in GetRemoteFolders())
                {
                    var worldFolder = Path.Join(remote.FullName, "worlds", worldName);
                    if (Directory.Exists(worldFolder) && Directory.GetFiles(worldFolder, "*.fwl2").Any())
                    {
                        return new DirectoryInfo(worldFolder);
                    }
                }
            }
            catch
            {
                // Fall through and report the world as not found
            }

            return null;
        }

        public DirectoryInfo ImportCloudWorld(string worldName, DirectoryInfo destSaveFolder, bool move)
        {
            var source = GetCloudWorldFolder(worldName)
                ?? throw new DirectoryNotFoundException($"No Steam Cloud world folder found for '{worldName}'.");

            // Block only if a world by this name already exists locally (same check the dropdown uses).
            // The dest folder may still exist holding just Valheim's local minimap cache (cacheMinimap*)
            // for a cloud world that's been played in-game; that's safe to import the save files into.
            if (!destSaveFolder.IsWorldNameAvailable(worldName))
            {
                throw new IOException($"A world named '{worldName}' already exists in the local save folder.");
            }

            var dest = new DirectoryInfo(Path.Join(destSaveFolder.FullName, "worlds_local", worldName));
            var createdDest = !dest.Exists;

            try
            {
                CopyDirectory(source, dest);
            }
            catch
            {
                // Never leave a partial copy behind, but never destroy a pre-existing folder (its minimap cache)
                if (createdDest && dest.Exists) dest.Delete(true);
                throw;
            }

            if (move)
            {
                try
                {
                    source.Delete(true);
                }
                catch (Exception e)
                {
                    // The copy is already usable, so treat a locked source (e.g. Steam still running) as success
                    Logger.Warning(e, "Copied cloud world '{world}' but could not delete the Steam Cloud source", worldName);
                }
            }

            return dest;
        }

        #region Helper methods

        private IEnumerable<DirectoryInfo> GetRemoteFolders()
        {
            var steamPath = GetSteamPath();
            if (steamPath == null) yield break;

            var userdata = Path.Join(steamPath, "userdata");
            if (!Directory.Exists(userdata)) yield break;

            // Each Steam account has its own userdata subfolder; a world may live under any of them
            foreach (var account in Directory.GetDirectories(userdata))
            {
                var remote = Path.Join(account, Resources.ValheimSteamAppId, "remote");
                if (Directory.Exists(remote))
                {
                    yield return new DirectoryInfo(remote);
                }
            }
        }

        private string GetSteamPath()
        {
            if (SteamPathResolved) return SteamPath;
            SteamPathResolved = true;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                var value = key?.GetValue("SteamPath")?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    SteamPath = Environment.ExpandEnvironmentVariables(value);
                }
            }
            catch (SecurityException e)
            {
                Logger.Warning(e, "No registry access to locate the Steam install path");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to read the Steam install path from the registry");
            }

            return SteamPath;
        }

        private static void CopyDirectory(DirectoryInfo source, DirectoryInfo dest)
        {
            dest.Create();

            foreach (var file in source.GetFiles())
            {
                file.CopyTo(Path.Join(dest.FullName, file.Name), true);
            }

            foreach (var subDir in source.GetDirectories())
            {
                CopyDirectory(subDir, new DirectoryInfo(Path.Join(dest.FullName, subDir.Name)));
            }
        }

        #endregion
    }
}
