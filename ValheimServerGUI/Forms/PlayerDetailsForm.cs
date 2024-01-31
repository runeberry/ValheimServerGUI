using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.Forms
{
    public partial class PlayerDetailsForm : Form
    {
        //private const string CharacterMatchConfidentSuffix = " (!)";
        private const string NotAvailable = "N/A";
        private const string ValidatePlayerNameText = "Name must be between 0-64 characters.";

        private readonly IPlayerDataRepository PlayerDataProvider;

        private PlayerInfo Player;

        private bool FormDirty;

        public PlayerDetailsForm(IPlayerDataRepository playerDataProvider)
        {
            PlayerDataProvider = playerDataProvider;

            InitializeComponent();
            this.AddApplicationIcon();

            PlayerNameEditButton.EditFunction = ButtonEditPlayerName_Click;
            SteamIdCopyButton.CopyFunction = () => Player?.PlayerId;

            CharacterListField.ItemsChanged += CharacterListField_ItemsChanged;
            CharacterListField.AddFunction = CharacterListFieldAdd_Click;
            CharacterListField.EditFunction = CharacterListFieldEdit_Click;
            CharacterListField.RemoveFunction = CharacterListFieldRemove_Click;

            ButtonRefresh.Click += ButtonRefresh_Click;
            ButtonOK.Click += ButtonOK_Click;

            FormDirty = false;
        }

        public void SetPlayerData(PlayerInfo player)
        {
            // Pull fresh data from provider
            Player = PlayerDataProvider.FindById(player.Key);
            RefreshPlayerData();

            FormDirty = false;
        }

        #region Form events

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.CloseReason != CloseReason.UserClosing) return;

            if (FormDirty)
            {
                var result = MessageBox.Show(
                    "Save changes to player data?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);

                switch (result)
                {
                    case DialogResult.Yes:
                        SavePlayerData();
                        break;
                    case DialogResult.No:
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void ButtonEditPlayerName_Click()
        {
            var result = TextPromptPopout.Prompt(
                "Edit Player Name",
                $"Enter the player's {Player.Platform} username:",
                Player.PlayerName,
                ValidatePlayerNameText,
                ValidatePlayerName);

            if (result == null || result == PlayerNameField.Value) return;

            PlayerNameField.Value = result;
            FormDirty = true;
        }

        private void CharacterListField_ItemsChanged()
        {
            FormDirty = true;
        }

        private string CharacterListFieldAdd_Click()
        {
            return TextPromptPopout.Prompt(
                "Add Character",
                "Add a Valheim character name for this player:",
                null,
                ValidatePlayerNameText,
                ValidatePlayerName);
        }

        private string CharacterListFieldEdit_Click(string selected)
        {
            return TextPromptPopout.Prompt(
                "Edit Character",
                $"Edit the name for character '{selected}'",
                ParseCharacterName(selected),
                ValidatePlayerNameText,
                ValidatePlayerName);
        }

        private bool CharacterListFieldRemove_Click(string selected)
        {
            // todo: Do I need to check if they're currently playing this character?
            // Will removing a character mess up logging/status updates?
            return true;
        }

        private void ButtonRefresh_Click(object sender, EventArgs e)
        {
            RefreshPlayerData();
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            SavePlayerData();
            Close();
        }

        #endregion

        #region Non-public methods

        private void RefreshPlayerData()
        {
            if (Player == null) return;

            PlayerNameField.Value = Player.PlayerName ?? NotAvailable;
            PlatformIdField.Value = Player.PlayerId;
            ZdoIdField.Value = Player.ZdoId ?? NotAvailable;
            CurrentCharacterNameField.Value = Player.LastStatusCharacter ?? NotAvailable;
            OnlineStatusField.Value = Player.PlayerStatus.ToString();
            StatusChangedField.Value = new TimeAgo(Player.LastStatusChange).ToString();

            var characterNames = Player.Characters.Select(GetCharacterDisplayName);
            CharacterListField.SetItems(characterNames);

            if (Player.Platform == PlayerPlatforms.Steam)
            {
                PlatformIcon.Image = Resources.Steam_16x;
            }
            else if (Player.Platform == PlayerPlatforms.Xbox)
            {
                PlatformIcon.Image = Resources.XboxLive_16x;
            }
        }

        private void SavePlayerData()
        {
            if (!FormDirty) return;

            var newCharacters = new List<PlayerInfo.CharacterInfo>();
            foreach (var characterName in CharacterListField.GetItems())
            {
                var existing = Player.Characters?.FirstOrDefault(c => c.CharacterName == characterName);
                if (existing != null)
                {
                    newCharacters.Add(existing);
                }
                else
                {
                    newCharacters.Add(new()
                    {
                        CharacterName = characterName,
                        MatchConfident = true,
                    });
                }
            }

            Player.PlayerName = PlayerNameField.Value;
            Player.Characters = newCharacters;

            PlayerDataProvider.Upsert(Player);

            FormDirty = false;
        }

        private bool ValidatePlayerName(string input)
        {
            // This is just a precaution, not based on any specification
            return !string.IsNullOrWhiteSpace(input) && input.Length <= 64;
        }

        private string GetCharacterDisplayName(PlayerInfo.CharacterInfo characterInfo)
        {
            var characterName = characterInfo.CharacterName;

            //if (!characterInfo.MatchConfident)
            //{
            //    characterName += CharacterMatchConfidentSuffix;
            //}

            return characterName;
        }

        private string ParseCharacterName(string characterName)
        {
            //if (characterName.EndsWith(CharacterMatchConfidentSuffix))
            //{
            //    characterName = characterName[..CharacterMatchConfidentSuffix.Length];
            //}

            return characterName;
        }

        #endregion
    }
}
