using System;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class PlayerDetailsForm : Form
    {
        private readonly IPlayerDataRepository PlayerDataProvider;

        private PlayerInfo Player;

        public PlayerDetailsForm(IPlayerDataRepository playerDataProvider)
        {
            PlayerDataProvider = playerDataProvider;

            InitializeComponent();
            this.AddApplicationIcon();

            ButtonRefresh.Click += ButtonRefresh_Click;
            ButtonOK.Click += ButtonOK_Click;
            SteamIdCopyButton.CopyFunction = () => Player?.SteamId;
        }

        private void ButtonRefresh_Click(object sender, EventArgs e)
        {
            RefreshPlayerData();
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            Close();
        }

        public void SetPlayerData(PlayerInfo player)
        {
            // Pull fresh data from provider
            Player = PlayerDataProvider.FindById(player.Key);
            RefreshPlayerData();
        }

        public void RefreshPlayerData()
        {
            if (Player == null) return;

            PlayerNameField.Value = Player.PlayerName ?? "(unknown)";
            SteamIdField.Value = Player.SteamId;
            ZdoIdField.Value = Player.ZdoId ?? "N/A";
            OnlineStatusField.Value = Player.PlayerStatus.ToString();
            StatusChangedField.Value = new TimeAgo(Player.LastStatusChange).ToString();

            SteamIdWarningIcon.Visible = !Player.MatchConfident;
        }
    }
}
