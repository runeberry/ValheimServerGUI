using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class MainWindow
    {
        private const int DiscordColorOnline = 0x57F287;
        private const int DiscordColorOffline = 0xED4245;
        private const int DiscordColorPlayerJoin = 0x5865F2;
        private const int DiscordColorPlayerLeave = 0xFEE75C;
        private const int DiscordColorJoinCode = 0xEB459E;

        private bool DiscordStatusIntegrationInitialized;
        private ServerStatus? LastDiscordFinalStatus;
        private ToolStripMenuItem DiscordStatusMenuItem;

        private string CurrentDiscordJoinCode;
        private bool DiscordOnlineAnnouncementSent;
        private bool DiscordPlayerNotificationsEnabledForSession;
        private int DiscordSessionGeneration;

        private readonly HashSet<string> DiscordOnlinePlayerKeys = new();
        private readonly Dictionary<string, DateTimeOffset> DiscordPlayerJoinTimes = new();

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (DiscordStatusIntegrationInitialized)
            {
                return;
            }

            DiscordStatusIntegrationInitialized = true;
            LastDiscordFinalStatus = Server.Status;

            Server.StatusChanged += this.BuildEventHandler<ServerStatus>(OnDiscordServerStatusChanged);
            Server.InviteCodeReady += this.BuildEventHandler<string>(OnDiscordInviteCodeReady);
            Server.PlayerDied += this.BuildEventHandler<string>(OnDiscordPlayerDied);
            PlayerDataProvider.PlayerStatusChanged += this.BuildEventHandler<PlayerInfo>(OnDiscordPlayerStatusChanged);

            InitializeDiscordStatusMenu();
        }

        private void InitializeDiscordStatusMenu()
        {
            var settingsItem = new ToolStripMenuItem("Settings...");
            settingsItem.Click += (_, _) =>
            {
                using var form = new DiscordSettingsForm(UserPrefsProvider);
                form.ShowDialog(this);
            };

            DiscordStatusMenuItem = new ToolStripMenuItem("&Discord Status");
            DiscordStatusMenuItem.DropDownItems.Add(settingsItem);

            // Insert immediately before Help.
            var insertIndex = Math.Max(0, MenuStrip.Items.Count - 1);
            MenuStrip.Items.Insert(insertIndex, DiscordStatusMenuItem);
        }

        private void OnDiscordServerStatusChanged(ServerStatus status)
        {
            if (status == ServerStatus.Starting)
            {
                DiscordSessionGeneration++;
                CurrentDiscordJoinCode = null;
                DiscordOnlineAnnouncementSent = false;
                DiscordPlayerNotificationsEnabledForSession = false;
                DiscordOnlinePlayerKeys.Clear();
                DiscordPlayerJoinTimes.Clear();
                return;
            }

            if (status == ServerStatus.Stopping)
            {
                // Avoid a flood of "player left" messages caused by normal server shutdown.
                DiscordPlayerNotificationsEnabledForSession = false;
                return;
            }

            if (status == ServerStatus.Running)
            {
                if (LastDiscordFinalStatus == ServerStatus.Running)
                {
                    return;
                }

                LastDiscordFinalStatus = ServerStatus.Running;
                DiscordPlayerNotificationsEnabledForSession = true;

                var generation = DiscordSessionGeneration;

                if (ServerCrossplayField.Value)
                {
                    // Give PlayFab a few seconds to provide the join code so the
                    // main "online" message can include it.
                    _ = SendDiscordOnlineAfterJoinCodeGracePeriodAsync(generation);
                }
                else
                {
                    _ = SendDiscordServerOnlineAsync(generation);
                }

                return;
            }

            if (status == ServerStatus.Stopped)
            {
                DiscordPlayerNotificationsEnabledForSession = false;
                DiscordSessionGeneration++;

                if (LastDiscordFinalStatus != ServerStatus.Running)
                {
                    LastDiscordFinalStatus = ServerStatus.Stopped;
                    CurrentDiscordJoinCode = null;
                    DiscordOnlineAnnouncementSent = false;
                    DiscordOnlinePlayerKeys.Clear();
                    DiscordPlayerJoinTimes.Clear();
                    return;
                }

                LastDiscordFinalStatus = ServerStatus.Stopped;
                _ = SendDiscordServerOfflineAsync();

                CurrentDiscordJoinCode = null;
                DiscordOnlineAnnouncementSent = false;
                DiscordOnlinePlayerKeys.Clear();
                DiscordPlayerJoinTimes.Clear();
            }
        }

        private async Task SendDiscordOnlineAfterJoinCodeGracePeriodAsync(int generation)
        {
            await Task.Delay(TimeSpan.FromSeconds(4));

            if (generation != DiscordSessionGeneration ||
                Server.Status != ServerStatus.Running ||
                DiscordOnlineAnnouncementSent)
            {
                return;
            }

            await SendDiscordServerOnlineAsync(generation);
        }

        private void OnDiscordInviteCodeReady(string inviteCode)
        {
            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                return;
            }

            var previousCode = CurrentDiscordJoinCode;
            CurrentDiscordJoinCode = inviteCode.Trim();

            if (Server.Status != ServerStatus.Running)
            {
                return;
            }

            if (!DiscordOnlineAnnouncementSent)
            {
                _ = SendDiscordServerOnlineAsync(DiscordSessionGeneration);
                return;
            }

            if (!string.Equals(previousCode, CurrentDiscordJoinCode, StringComparison.Ordinal))
            {
                _ = SendDiscordJoinCodeReadyAsync();
            }
        }

        private void OnDiscordPlayerDied(string playerName)
        {
            if (!DiscordPlayerNotificationsEnabledForSession ||
                Server.Status != ServerStatus.Running ||
                string.IsNullOrWhiteSpace(playerName))
            {
                return;
            }

            _ = SendDiscordPlayerDiedAsync(playerName.Trim());
        }

        private void OnDiscordPlayerStatusChanged(PlayerInfo player)
        {
            if (player == null || !DiscordPlayerNotificationsEnabledForSession)
            {
                return;
            }

            if (Server.Status != ServerStatus.Running)
            {
                return;
            }

            var key = GetDiscordPlayerKey(player);

            if (player.PlayerStatus == PlayerStatus.Online)
            {
                if (!DiscordOnlinePlayerKeys.Add(key))
                {
                    return;
                }

                DiscordPlayerJoinTimes[key] = DateTimeOffset.UtcNow;
                _ = SendDiscordPlayerJoinedAsync(player);
                return;
            }

            if (player.PlayerStatus == PlayerStatus.Offline)
            {
                // Only announce a leave if we actually saw this player become Online
                // during this server session. This prevents wrong-password attempts
                // and stale player records from creating false leave messages.
                if (!DiscordOnlinePlayerKeys.Remove(key))
                {
                    return;
                }

                DiscordPlayerJoinTimes.TryGetValue(key, out var joinedAt);
                DiscordPlayerJoinTimes.Remove(key);

                _ = SendDiscordPlayerLeftAsync(player, joinedAt);
            }
        }

        private async Task SendDiscordServerOnlineAsync(int generation)
        {
            if (generation != DiscordSessionGeneration ||
                Server.Status != ServerStatus.Running ||
                DiscordOnlineAnnouncementSent)
            {
                return;
            }

            var prefs = UserPrefsProvider.LoadPreferences();

            if (!prefs.DiscordStatusNotifications ||
                !prefs.DiscordNotifyServerOnline ||
                string.IsNullOrWhiteSpace(prefs.DiscordWebhookUrl))
            {
                return;
            }

            try
            {
                DiscordOnlineAnnouncementSent = true;

                var serverName = GetDiscordServerName();
                var worldName = GetDiscordWorldName();
                var address = GetDiscordConnectionAddress();
                var uptime = FormatDiscordDuration(ServerUptimeTimer.Elapsed);
                var crossplay = ServerCrossplayField.Value ? "Enabled" : "Disabled";
                var joinCode = ServerCrossplayField.Value
                    ? (string.IsNullOrWhiteSpace(CurrentDiscordJoinCode) ? "Loading..." : CurrentDiscordJoinCode)
                    : "N/A";

                await DiscordWebhookClient.SendEmbedAsync(
                    prefs.DiscordWebhookUrl,
                    "🟢 Valheim Server Online",
                    $"**{serverName}** is ready.",
                    DiscordColorOnline,
                    ("World", worldName, true),
                    ("Address", address, true),
                    ("Join Code", joinCode, true),
                    ("Crossplay", crossplay, true),
                    ("Players", $"{DiscordOnlinePlayerKeys.Count}", true),
                    ("Uptime", uptime, true));

                Logger.Information("Discord server online notification sent");
            }
            catch (Exception exception)
            {
                DiscordOnlineAnnouncementSent = false;
                Logger.Error(exception, "Failed to send Discord server online notification");
            }
        }

        private async Task SendDiscordServerOfflineAsync()
        {
            var prefs = UserPrefsProvider.LoadPreferences();

            if (!prefs.DiscordStatusNotifications ||
                !prefs.DiscordNotifyServerOffline ||
                string.IsNullOrWhiteSpace(prefs.DiscordWebhookUrl))
            {
                return;
            }

            try
            {
                await DiscordWebhookClient.SendEmbedAsync(
                    prefs.DiscordWebhookUrl,
                    "🔴 Valheim Server Offline",
                    $"**{GetDiscordServerName()}** has stopped.",
                    DiscordColorOffline,
                    ("World", GetDiscordWorldName(), true),
                    ("Address", GetDiscordConnectionAddress(), true),
                    ("Uptime", FormatDiscordDuration(ServerUptimeTimer.Elapsed), true));

                Logger.Information("Discord server offline notification sent");
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to send Discord server offline notification");
            }
        }

        private async Task SendDiscordJoinCodeReadyAsync()
        {
            var prefs = UserPrefsProvider.LoadPreferences();

            if (!prefs.DiscordStatusNotifications ||
                !prefs.DiscordNotifyJoinCode ||
                string.IsNullOrWhiteSpace(prefs.DiscordWebhookUrl))
            {
                return;
            }

            try
            {
                await DiscordWebhookClient.SendEmbedAsync(
                    prefs.DiscordWebhookUrl,
                    "🔑 Join Code Ready",
                    $"**{GetDiscordServerName()}** has a new join code.",
                    DiscordColorJoinCode,
                    ("Join Code", CurrentDiscordJoinCode, true),
                    ("Address", GetDiscordConnectionAddress(), true),
                    ("Uptime", FormatDiscordDuration(ServerUptimeTimer.Elapsed), true));

                Logger.Information("Discord join code notification sent");
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to send Discord join code notification");
            }
        }

        private async Task SendDiscordPlayerDiedAsync(string playerName)
        {
            var prefs = UserPrefsProvider.LoadPreferences();

            if (!prefs.DiscordStatusNotifications ||
                !prefs.DiscordNotifyPlayerDied ||
                string.IsNullOrWhiteSpace(prefs.DiscordWebhookUrl))
            {
                return;
            }

            try
            {
                await DiscordWebhookClient.SendAsync(
                    prefs.DiscordWebhookUrl,
                    $"💀 RIP **{playerName}** 🪦");

                Logger.Information(
                    "Discord player death notification sent: {player}",
                    playerName);
            }
            catch (Exception exception)
            {
                Logger.Error(
                    exception,
                    "Failed to send Discord player death notification");
            }
        }

        private async Task SendDiscordPlayerJoinedAsync(PlayerInfo player)
        {
            var prefs = UserPrefsProvider.LoadPreferences();

            if (!prefs.DiscordStatusNotifications ||
                !prefs.DiscordNotifyPlayerJoined ||
                string.IsNullOrWhiteSpace(prefs.DiscordWebhookUrl))
            {
                return;
            }

            try
            {
                await DiscordWebhookClient.SendEmbedAsync(
                    prefs.DiscordWebhookUrl,
                    "👤 Player Joined",
                    $"**{GetDiscordPlayerDisplayName(player)}** joined the server.",
                    DiscordColorPlayerJoin,
                    ("Players Online", $"{DiscordOnlinePlayerKeys.Count}", true),
                    ("Server Uptime", FormatDiscordDuration(ServerUptimeTimer.Elapsed), true),
                    ("World", GetDiscordWorldName(), true));

                Logger.Information(
                    "Discord player joined notification sent: {player}",
                    GetDiscordPlayerDisplayName(player));
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to send Discord player joined notification");
            }
        }

        private async Task SendDiscordPlayerLeftAsync(PlayerInfo player, DateTimeOffset joinedAt)
        {
            var prefs = UserPrefsProvider.LoadPreferences();

            if (!prefs.DiscordStatusNotifications ||
                !prefs.DiscordNotifyPlayerLeft ||
                string.IsNullOrWhiteSpace(prefs.DiscordWebhookUrl))
            {
                return;
            }

            try
            {
                var sessionDuration = joinedAt == default
                    ? "Unknown"
                    : FormatDiscordDuration(DateTimeOffset.UtcNow - joinedAt);

                await DiscordWebhookClient.SendEmbedAsync(
                    prefs.DiscordWebhookUrl,
                    "👋 Player Left",
                    $"**{GetDiscordPlayerDisplayName(player)}** left the server.",
                    DiscordColorPlayerLeave,
                    ("Players Online", $"{DiscordOnlinePlayerKeys.Count}", true),
                    ("Session Time", sessionDuration, true),
                    ("Server Uptime", FormatDiscordDuration(ServerUptimeTimer.Elapsed), true));

                Logger.Information(
                    "Discord player left notification sent: {player}",
                    GetDiscordPlayerDisplayName(player));
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to send Discord player left notification");
            }
        }

        private string GetDiscordServerName()
        {
            var serverName = ServerNameField.Value;
            return string.IsNullOrWhiteSpace(serverName) ? "Valheim Server" : serverName;
        }

        private string GetDiscordWorldName()
        {
            var worldName = WorldSelectRadioExisting.Value
                ? WorldSelectExistingNameField.Value
                : WorldSelectNewNameField.Value;

            return string.IsNullOrWhiteSpace(worldName) ? "Unknown" : worldName;
        }

        private string GetDiscordConnectionAddress()
        {
            var ip = IpAddressProvider.ExternalIpAddress;

            if (string.IsNullOrWhiteSpace(ip))
            {
                ip = IpAddressProvider.InternalIpAddress;
            }

            if (string.IsNullOrWhiteSpace(ip))
            {
                ip = "Unavailable";
            }

            return $"{ip}:{ServerPortField.Value}";
        }

        private static string GetDiscordPlayerKey(PlayerInfo player)
        {
            if (!string.IsNullOrWhiteSpace(player.Key))
            {
                return player.Key;
            }

            return $"{player.Platform}:{player.PlayerId}:{player.LastStatusCharacter}";
        }

        private static string GetDiscordPlayerDisplayName(PlayerInfo player)
        {
            if (!string.IsNullOrWhiteSpace(player.LastStatusCharacter))
            {
                return player.LastStatusCharacter;
            }

            if (!string.IsNullOrWhiteSpace(player.PlayerName))
            {
                return player.PlayerName;
            }

            if (!string.IsNullOrWhiteSpace(player.PlayerId))
            {
                return player.PlayerId;
            }

            return "Unknown Player";
        }

        private static string FormatDiscordDuration(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
            {
                duration = TimeSpan.Zero;
            }

            if (duration.TotalDays >= 1)
            {
                return $"{(int)duration.TotalDays}d {duration.Hours:00}h {duration.Minutes:00}m";
            }

            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}h {duration.Minutes:00}m {duration.Seconds:00}s";
            }

            return $"{duration.Minutes}m {duration.Seconds:00}s";
        }
    }
}
