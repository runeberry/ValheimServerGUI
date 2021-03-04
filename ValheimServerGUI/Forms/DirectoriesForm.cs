using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class DirectoriesForm : Form
    {
        private readonly UserPrefs UserPrefs;

        public DirectoriesForm()
        {
            InitializeComponent();
        }

        public DirectoriesForm(UserPrefs userPrefs) : this()
        {
            this.UserPrefs = userPrefs;

            InitializeFormFields();
        }

        private void InitializeFormFields()
        {
            this.GamePathField.Value = this.UserPrefs.GetEnvironmentValue(UserPrefsKeys.ValheimGamePath);
            this.ServerPathField.Value = this.UserPrefs.GetEnvironmentValue(UserPrefsKeys.ValheimServerPath);
            this.WorldsFolderField.Value = this.UserPrefs.GetEnvironmentValue(UserPrefsKeys.ValheimWorldsFolder);

            this.GamePathField.ConfigureFileDialog(dialog => dialog.Filter = "Applications (*.exe)|*.exe");
            this.ServerPathField.ConfigureFileDialog(dialog => dialog.Filter = "Applications (*.exe)|*.exe");
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            this.UserPrefs.SetValue(UserPrefsKeys.ValheimGamePath, this.GamePathField.Value);
            this.UserPrefs.SetValue(UserPrefsKeys.ValheimServerPath, this.ServerPathField.Value);
            this.UserPrefs.SetValue(UserPrefsKeys.ValheimWorldsFolder, this.WorldsFolderField.Value);
            this.UserPrefs.SaveFile();

            this.Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ButtonDefaults_Click(object sender, EventArgs e)
        {
            this.GamePathField.Value = UserPrefs.Default.GetEnvironmentValue(UserPrefsKeys.ValheimGamePath);
            this.ServerPathField.Value = UserPrefs.Default.GetEnvironmentValue(UserPrefsKeys.ValheimServerPath);
            this.WorldsFolderField.Value = UserPrefs.Default.GetEnvironmentValue(UserPrefsKeys.ValheimWorldsFolder);
        }
    }
}
