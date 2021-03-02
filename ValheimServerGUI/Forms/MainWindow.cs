using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class MainWindow : Form
    {
        private ValheimServer Server;

        public MainWindow()
        {
            InitializeComponent();

            InitializeMenuItems();
            InitializeServer();
            InitializeGameData();

            this.SetStatusText("Loaded OK");
        }

        #region Initialization

        private void InitializeMenuItems()
        {
            this.MenuItemFileDirectories.Click += new EventHandler(this.MenuItemFileDirectories_Clicked);
            this.MenuItemFileClose.Click += new EventHandler(this.MenuItemFileClose_Clicked);

            this.MenuItemHelpUpdates.Click += new EventHandler(this.MenuItemHelpUpdates_Clicked);
            this.MenuItemHelpAbout.Click += new EventHandler(this.MenuItemHelpAbout_Clicked);
        }

        private void InitializeServer()
        {
            Server = new ValheimServer();
            Server.FilteredLogger.LogReceived += new EventHandler<LogEvent>(this.OnLogReceived);
        }

        private void InitializeGameData()
        {
            var worlds = ValheimData.GetWorldNames();

            if (!worlds.Any())
            {
                ComboBoxWorldSelect.DataSource = new List<string> { "(no worlds found)" };
            }
            else
            {
                ComboBoxWorldSelect.DataSource = worlds;
            }
        }

        private void OnLogReceived(object sender, LogEvent logEvent)
        {
            // Allows cross-thread access to the TextBox
            // See here: https://stackoverflow.com/questions/519233/writing-to-a-textbox-from-another-thread
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object, LogEvent>(OnLogReceived), new object[] { sender, logEvent });
                return;
            }

            this.AddLog(logEvent.Message);
        }

        #endregion

        #region Form Event Handlers

        private void ButtonStartServer_Click(object sender, EventArgs e)
        {
            Server.ServerPath = @"C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server\valheim_server.exe";
            Server.ServerName = this.GetServerName();
            Server.ServerPassword = this.GetServerPassword();
            Server.WorldName = this.GetSelectedWorldName();
            Server.Public = this.GetPublicSelection();

            try
            {
                Server.Validate();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    exception.Message,
                    "Server Configuration Error",
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);

                return;
            }

            this.SetStatusText("Starting server...");
            Server.Start();
        }

        private void ButtonStopServer_Click(object sender, EventArgs e)
        {
            this.SetStatusText("Stopping server...");

            Server.Stop();
        }

        private void ButtonClearLogs_Click(object sender, EventArgs e)
        {
            this.ClearLogs();
        }

        #endregion

        #region Menu Items

        private void MenuItemFileDirectories_Clicked(object sender, EventArgs e)
        {
            this.SetStatusText("Clicked Directories!");
        }

        private void MenuItemFileClose_Clicked(object sender, EventArgs e)
        {
            this.CloseApplication();
        }

        private void MenuItemHelpUpdates_Clicked(object sender, EventArgs e)
        {
            this.SetStatusText("Clicked Updates!");
        }

        private void MenuItemHelpAbout_Clicked(object sender, EventArgs e)
        {
            this.SetStatusText("Clicked About!");
        }

        #endregion

        #region Common Methods

        private string GetServerName()
        {
            return this.TextBoxServerName.Text;
        }

        private string GetServerPassword()
        {
            return this.TextBoxPassword.Text;
        }

        private string GetSelectedWorldName()
        {
            return this.ComboBoxWorldSelect.SelectedItem.ToString();
        }

        private bool GetPublicSelection()
        {
            return this.CheckBoxPublic.Checked;
        }

        private void AddLog(string message)
        {
            this.TextBoxLogs.AppendLine(message);
        }

        private void ClearLogs()
        {
            this.TextBoxLogs.Text = "";
        }

        private void SetStatusText(string message)
        {
            this.StatusStripLabel.Text = message;
        }

        private void CloseApplication()
        {
            Server.Stop();
            Application.Exit();
        }

        #endregion
    }
}
