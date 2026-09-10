using System;
using System.IO;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class MainWindow
    {
        private ToolStripMenuItem MenuItemBackups;
        private ToolStripMenuItem MenuItemBackupsCreate;
        private ToolStripMenuItem MenuItemBackupsRestore;
        private ToolStripMenuItem MenuItemBackupsOpenFolder;

        /// <summary>
        /// Adds manual backup controls without modifying MainWindow.Designer.cs.
        /// </summary>
        private void InitializeBackupMenu()
        {
            MenuItemBackups = new ToolStripMenuItem("&Backups");
            MenuItemBackupsCreate = new ToolStripMenuItem("&Backup World Now...");
            MenuItemBackupsRestore = new ToolStripMenuItem("&Restore Backup...");
            MenuItemBackupsOpenFolder = new ToolStripMenuItem("&Open Backups Folder");

            MenuItemBackupsCreate.Click += MenuItemBackupsCreate_Click;
            MenuItemBackupsRestore.Click += MenuItemBackupsRestore_Click;
            MenuItemBackupsOpenFolder.Click += MenuItemBackupsOpenFolder_Click;

            MenuItemBackups.DropDownItems.Add(MenuItemBackupsCreate);
            MenuItemBackups.DropDownItems.Add(MenuItemBackupsRestore);
            MenuItemBackups.DropDownItems.Add(new ToolStripSeparator());
            MenuItemBackups.DropDownItems.Add(MenuItemBackupsOpenFolder);

            // Insert before Help so the main menu becomes File | Backups | Help.
            var helpIndex = MenuStrip.Items.IndexOf(MenuItemHelp);
            if (helpIndex >= 0)
                MenuStrip.Items.Insert(helpIndex, MenuItemBackups);
            else
                MenuStrip.Items.Add(MenuItemBackups);
        }

        private bool TryGetBackupContext(out DirectoryInfo saveDataFolder, out string worldName)
        {
            saveDataFolder = null;
            worldName = null;

            if (!Server.IsAnyStatus(ServerStatus.Stopped))
            {
                MessageBox.Show(
                    "Manual backup and restore are only available while the server is stopped.\r\n\r\n" +
                    "Stop the server first so every world file is in a consistent state.",
                    "Server Must Be Stopped",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            if (!WorldSelectRadioExisting.Value || string.IsNullOrWhiteSpace(WorldSelectExistingNameField.Value))
            {
                MessageBox.Show(
                    "Select an existing world first.",
                    "No World Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                var options = GetServerOptionsFromFormState();
                saveDataFolder = options.GetValidatedSaveDataFolder();
                worldName = WorldSelectExistingNameField.Value;
                return true;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Unable to prepare world backup operation");
                MessageBox.Show(
                    exception.Message,
                    "Backup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private async void MenuItemBackupsCreate_Click(object sender, EventArgs e)
        {
            if (!TryGetBackupContext(out var saveDataFolder, out var worldName)) return;

            MenuItemBackupsCreate.Enabled = false;
            MenuItemBackupsRestore.Enabled = false;

            try
            {
                Cursor = Cursors.WaitCursor;
                var backup = await System.Threading.Tasks.Task.Run(
                    () => WorldBackupManager.CreateBackup(saveDataFolder, worldName));

                Logger.Information("Created manual backup for world '{world}' at {path}", worldName, backup.FullName);

                var result = MessageBox.Show(
                    $"Backup created successfully.\r\n\r\n{backup.FullName}\r\n\r\nOpen the backup folder?",
                    "Backup Complete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                    OpenHelper.OpenDirectory(backup.DirectoryName);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to create manual backup for world '{world}'", worldName);
                MessageBox.Show(
                    $"Backup failed:\r\n\r\n{exception.Message}",
                    "Backup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                MenuItemBackupsCreate.Enabled = true;
                MenuItemBackupsRestore.Enabled = true;
            }
        }

        private async void MenuItemBackupsRestore_Click(object sender, EventArgs e)
        {
            if (!TryGetBackupContext(out var saveDataFolder, out var worldName)) return;

            var worldBackupDir = WorldBackupManager.GetWorldBackupDirectory(worldName);
            using var dialog = new OpenFileDialog
            {
                Title = $"Restore backup for {worldName}",
                Filter = "ValheimServerGUI Backups (*.zip)|*.zip|All Files (*.*)|*.*",
                InitialDirectory = worldBackupDir.FullName,
                CheckFileExists = true,
                Multiselect = false,
                RestoreDirectory = true,
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            var confirm = MessageBox.Show(
                $"Restore this backup to world '{worldName}'?\r\n\r\n" +
                $"{dialog.FileName}\r\n\r\n" +
                "The current world will be replaced. A PRE-RESTORE safety backup will be created automatically before any files are changed.",
                "Confirm World Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes) return;

            MenuItemBackupsCreate.Enabled = false;
            MenuItemBackupsRestore.Enabled = false;

            try
            {
                Cursor = Cursors.WaitCursor;
                var backupFile = new FileInfo(dialog.FileName);
                var safetyBackup = await System.Threading.Tasks.Task.Run(
                    () => WorldBackupManager.RestoreBackup(saveDataFolder, worldName, backupFile));

                RefreshWorldSelect();
                WorldSelectRadioExisting.Value = true;
                WorldSelectExistingNameField.Value = worldName;

                Logger.Information(
                    "Restored world '{world}' from {backup}; pre-restore backup: {safety}",
                    worldName,
                    backupFile.FullName,
                    safetyBackup.FullName);

                MessageBox.Show(
                    $"World restored successfully.\r\n\r\n" +
                    $"Safety backup of the previous state:\r\n{safetyBackup.FullName}",
                    "Restore Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to restore backup for world '{world}'", worldName);
                MessageBox.Show(
                    $"Restore failed:\r\n\r\n{exception.Message}\r\n\r\n" +
                    "If a pre-restore backup was created, it remains in the Backups folder.",
                    "Restore Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                MenuItemBackupsCreate.Enabled = true;
                MenuItemBackupsRestore.Enabled = true;
            }
        }

        private void MenuItemBackupsOpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                OpenHelper.OpenDirectory(WorldBackupManager.GetBackupRoot().FullName);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Unable to open manual backup folder");
                MessageBox.Show(
                    exception.Message,
                    "Backup Folder Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
