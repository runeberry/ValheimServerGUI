using System;
using System.Drawing;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public class DiscordSettingsForm : Form
    {
        private readonly IUserPreferencesProvider UserPrefsProvider;

        private readonly CheckBox EnableNotificationsCheckBox;
        private readonly TextBox WebhookUrlTextBox;
        private readonly CheckBox ShowWebhookCheckBox;

        private readonly CheckBox NotifyServerOnlineCheckBox;
        private readonly CheckBox NotifyServerOfflineCheckBox;
        private readonly CheckBox NotifyPlayerJoinedCheckBox;
        private readonly CheckBox NotifyPlayerLeftCheckBox;
        private readonly CheckBox NotifyPlayerDiedCheckBox;
        private readonly CheckBox NotifyJoinCodeCheckBox;

        private readonly Button TestButton;
        private readonly Button SaveButton;
        private readonly Button CancelButtonControl;

        public DiscordSettingsForm(IUserPreferencesProvider userPrefsProvider)
        {
            UserPrefsProvider = userPrefsProvider ?? throw new ArgumentNullException(nameof(userPrefsProvider));

            Text = "Discord Status Notifications";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(640, 430);

            EnableNotificationsCheckBox = new CheckBox
            {
                AutoSize = true,
                Location = new Point(18, 18),
                Text = "Enable Discord notifications",
            };
            EnableNotificationsCheckBox.CheckedChanged += (_, _) => UpdateNotificationControlsEnabled();

            var webhookLabel = new Label
            {
                AutoSize = true,
                Location = new Point(18, 52),
                Text = "Discord webhook URL:",
            };

            WebhookUrlTextBox = new TextBox
            {
                Location = new Point(18, 73),
                Size = new Size(600, 23),
                UseSystemPasswordChar = true,
            };

            ShowWebhookCheckBox = new CheckBox
            {
                AutoSize = true,
                Location = new Point(18, 103),
                Text = "Show webhook URL",
            };
            ShowWebhookCheckBox.CheckedChanged += (_, _) =>
                WebhookUrlTextBox.UseSystemPasswordChar = !ShowWebhookCheckBox.Checked;

            var notificationsGroup = new GroupBox
            {
                Location = new Point(18, 137),
                Size = new Size(600, 200),
                Text = "Notifications",
            };

            NotifyServerOnlineCheckBox = CreateNotificationCheckBox(
                "Server online", 18, 30);
            NotifyServerOfflineCheckBox = CreateNotificationCheckBox(
                "Server offline", 18, 63);
            NotifyPlayerJoinedCheckBox = CreateNotificationCheckBox(
                "Player joined", 18, 96);
            NotifyPlayerLeftCheckBox = CreateNotificationCheckBox(
                "Player left", 300, 30);
            NotifyPlayerDiedCheckBox = CreateNotificationCheckBox(
                "Player died (RIP)", 300, 63);
            NotifyJoinCodeCheckBox = CreateNotificationCheckBox(
                "Join code ready / changed", 300, 96);

            var joinCodeInfo = new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Location = new Point(18, 140),
                Text = "Join code may still appear inside the Server Online message; this option controls the separate join-code alert.",
            };

            notificationsGroup.Controls.Add(NotifyServerOnlineCheckBox);
            notificationsGroup.Controls.Add(NotifyServerOfflineCheckBox);
            notificationsGroup.Controls.Add(NotifyPlayerJoinedCheckBox);
            notificationsGroup.Controls.Add(NotifyPlayerLeftCheckBox);
            notificationsGroup.Controls.Add(NotifyPlayerDiedCheckBox);
            notificationsGroup.Controls.Add(NotifyJoinCodeCheckBox);
            notificationsGroup.Controls.Add(joinCodeInfo);

            var infoLabel = new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Location = new Point(18, 350),
                Text = "The webhook is stored locally in userprefs.json. Treat it like a password.",
            };

            TestButton = new Button
            {
                Location = new Point(18, 382),
                Size = new Size(120, 30),
                Text = "Test Webhook",
            };
            TestButton.Click += TestButton_Click;

            SaveButton = new Button
            {
                Location = new Point(440, 382),
                Size = new Size(85, 30),
                Text = "Save",
            };
            SaveButton.Click += SaveButton_Click;

            CancelButtonControl = new Button
            {
                Location = new Point(533, 382),
                Size = new Size(85, 30),
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
            };

            Controls.Add(EnableNotificationsCheckBox);
            Controls.Add(webhookLabel);
            Controls.Add(WebhookUrlTextBox);
            Controls.Add(ShowWebhookCheckBox);
            Controls.Add(notificationsGroup);
            Controls.Add(infoLabel);
            Controls.Add(TestButton);
            Controls.Add(SaveButton);
            Controls.Add(CancelButtonControl);

            AcceptButton = SaveButton;
            CancelButton = CancelButtonControl;

            LoadCurrentPreferences();
            UpdateNotificationControlsEnabled();
        }

        private static CheckBox CreateNotificationCheckBox(string text, int x, int y)
        {
            return new CheckBox
            {
                AutoSize = true,
                Location = new Point(x, y),
                Text = text,
            };
        }

        private void UpdateNotificationControlsEnabled()
        {
            var enabled = EnableNotificationsCheckBox.Checked;

            NotifyServerOnlineCheckBox.Enabled = enabled;
            NotifyServerOfflineCheckBox.Enabled = enabled;
            NotifyPlayerJoinedCheckBox.Enabled = enabled;
            NotifyPlayerLeftCheckBox.Enabled = enabled;
            NotifyPlayerDiedCheckBox.Enabled = enabled;
            NotifyJoinCodeCheckBox.Enabled = enabled;
        }

        private void LoadCurrentPreferences()
        {
            var prefs = UserPrefsProvider.LoadPreferences();

            EnableNotificationsCheckBox.Checked = prefs.DiscordStatusNotifications;
            WebhookUrlTextBox.Text = prefs.DiscordWebhookUrl ?? string.Empty;

            NotifyServerOnlineCheckBox.Checked = prefs.DiscordNotifyServerOnline;
            NotifyServerOfflineCheckBox.Checked = prefs.DiscordNotifyServerOffline;
            NotifyPlayerJoinedCheckBox.Checked = prefs.DiscordNotifyPlayerJoined;
            NotifyPlayerLeftCheckBox.Checked = prefs.DiscordNotifyPlayerLeft;
            NotifyPlayerDiedCheckBox.Checked = prefs.DiscordNotifyPlayerDied;
            NotifyJoinCodeCheckBox.Checked = prefs.DiscordNotifyJoinCode;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            var prefs = UserPrefsProvider.LoadPreferences();

            prefs.DiscordStatusNotifications = EnableNotificationsCheckBox.Checked;
            prefs.DiscordWebhookUrl = WebhookUrlTextBox.Text.Trim();

            prefs.DiscordNotifyServerOnline = NotifyServerOnlineCheckBox.Checked;
            prefs.DiscordNotifyServerOffline = NotifyServerOfflineCheckBox.Checked;
            prefs.DiscordNotifyPlayerJoined = NotifyPlayerJoinedCheckBox.Checked;
            prefs.DiscordNotifyPlayerLeft = NotifyPlayerLeftCheckBox.Checked;
            prefs.DiscordNotifyPlayerDied = NotifyPlayerDiedCheckBox.Checked;
            prefs.DiscordNotifyJoinCode = NotifyJoinCodeCheckBox.Checked;

            UserPrefsProvider.SavePreferences(prefs);

            DialogResult = DialogResult.OK;
            Close();
        }

        private async void TestButton_Click(object sender, EventArgs e)
        {
            var webhookUrl = WebhookUrlTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                MessageBox.Show(
                    this,
                    "Enter a Discord webhook URL first.",
                    "Discord Webhook",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            TestButton.Enabled = false;

            try
            {
                await DiscordWebhookClient.SendAsync(
                    webhookUrl,
                    "✅ **ValheimServerGUI Discord webhook test successful.**");

                MessageBox.Show(
                    this,
                    "Test message sent successfully.",
                    "Discord Webhook",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    $"Failed to send the test message.{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                    "Discord Webhook",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                TestButton.Enabled = true;
            }
        }
    }
}
