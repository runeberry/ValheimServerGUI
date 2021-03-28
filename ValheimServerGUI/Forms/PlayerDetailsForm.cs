using System;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class PlayerDetailsForm : Form
    {
        private readonly IPlayerDataProvider PlayerDataProvider;

        private PlayerInfo Player;

        public PlayerDetailsForm(IPlayerDataProvider playerDataProvider)
        {
            this.PlayerDataProvider = playerDataProvider;

            InitializeComponent();

            this.ButtonRefresh.Click += ButtonRefresh_Click;
            this.ButtonOK.Click += ButtonOK_Click;
            this.SteamIdCopyIcon.Click += SteamIdCopyIcon_Click;
        }

        private void SteamIdCopyIcon_Click(object sender, EventArgs e)
        {
            if (this.Player?.SteamId != null)
            {
                Clipboard.SetText(this.Player.SteamId);
            }
        }

        private void ButtonRefresh_Click(object sender, EventArgs e)
        {
            this.RefreshPlayerData();
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void SetPlayerData(PlayerInfo player)
        {
            // Pull fresh data from provider
            Player = this.PlayerDataProvider.FindById(player.Key);
            this.RefreshPlayerData();
        }

        public void RefreshPlayerData()
        {
            if (this.Player == null) return;
            
            this.PlayerNameField.Value = this.Player.PlayerName ?? "(unknown)";
            this.SteamIdField.Value = this.Player.SteamId;
            this.ZdoIdField.Value = this.Player.ZdoId ?? "N/A";
            this.OnlineStatusField.Value = this.Player.PlayerStatus.ToString();
            this.StatusChangedField.Value = new TimeAgo(this.Player.LastStatusChange).ToString();

            this.SteamIdWarningIcon.Visible = !this.Player.SteamIdConfident;
        }
    }
}
