using System;
using System.Collections.Generic;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools.Data;
using ValheimServerGUI.Tools.Logging;

namespace ValheimServerGUI.Game
{
    public interface IPlayerDataProvider : ILocalDataProvider<PlayerInfo>
    {
        public event EventHandler<PlayerInfo> PlayerStatusChanged;
    }

    public class PlayerDataProvider : JsonLocalDataProvider<PlayerInfo>, IPlayerDataProvider
    {
        public event EventHandler<PlayerInfo> PlayerStatusChanged;

        private Dictionary<string, PlayerStatus> PlayerStatusMap = new();

        public PlayerDataProvider(ApplicationLogger logger) : base(logger, Resources.PlayerListFilePath)
        {
            this.DataLoaded += this.OnDataLoaded;
            this.EntityUpdated += this.OnEntityUpdated;

            this.Load();
        }

        private void OnDataLoaded(object sender, EventArgs args)
        {
            this.PlayerStatusMap.Clear();

            foreach (var player in this.Data)
            {
                this.PlayerStatusMap[player.Key] = player.PlayerStatus;
            }
        }

        private void OnEntityUpdated(object sender, PlayerInfo player)
        {
            bool statusChanged;

            if (this.PlayerStatusMap.TryGetValue(player.Key, out var oldStatus))
            {
                statusChanged = player.PlayerStatus != oldStatus;
            }
            else
            {
                statusChanged = true;
            }

            this.PlayerStatusMap[player.Key] = player.PlayerStatus;

            if (statusChanged)
            {
                this.PlayerStatusChanged?.Invoke(this, player);
            }
        }
    }
}
