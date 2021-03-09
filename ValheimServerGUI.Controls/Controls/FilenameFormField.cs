using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class FilenameFormField : UserControl, IFormField<string>
    {
        private Action<OpenFileDialog> FileDialogBuilder;
        private Action<FolderBrowserDialog> FolderDialogBuilder;

        public string LabelText
        {
            get => this.Label.Text;
            set => this.Label.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        public string HelpText
        {
            get => this.HelpLabel.Text;
            set => this.HelpLabel.Text = value;
        }

        public string Value
        {
            get => this.TextBox.Text;
            set => this.TextBox.Text = value;
        }

        /// <summary>
        /// Sets the initial directory that the file/folder dialog will open to.
        /// If null, will default to the directory of the current Value of this control.
        /// </summary>
        public string InitialPath { get; set; }

        public FileSelectMode FileSelectMode { get; set; }

        public string MultiFileSeparator { get; set; } = "; ";

        public FilenameFormField()
        {
            InitializeComponent();
        }

        public void ConfigureFileDialog(Action<OpenFileDialog> builder)
        {
            FileDialogBuilder = builder;
        }

        public void ConfigureFolderDialog(Action<FolderBrowserDialog> builder)
        {
            FolderDialogBuilder = builder;
        }

        private void FileBrowserButton_Click(object sender, EventArgs e)
        {
            string result;

            if (this.FileSelectMode == FileSelectMode.Directory)
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    if (this.FolderDialogBuilder != null)
                    {
                        this.FolderDialogBuilder(dialog);
                    }

                    dialog.RootFolder = Environment.SpecialFolder.ProgramFilesX86;
                    dialog.SelectedPath = !string.IsNullOrWhiteSpace(this.InitialPath)
                        ? new DirectoryInfo(this.InitialPath).FullName
                        : new DirectoryInfo(this.Value).FullName;

                    if (dialog.ShowDialog() != DialogResult.OK) return;

                    result = dialog.SelectedPath;
                }
            }
            else
            {
                using (var dialog = new OpenFileDialog())
                {
                    if (this.FileDialogBuilder != null)
                    {
                        this.FileDialogBuilder(dialog);
                    }

                    dialog.InitialDirectory = !string.IsNullOrWhiteSpace(this.InitialPath)
                        ? new FileInfo(this.InitialPath).Directory.FullName
                        : new FileInfo(this.Value).Directory.FullName;

                    // Override the Multiselect property based on this control's FileSelectMode
                    dialog.Multiselect = this.FileSelectMode == FileSelectMode.MultiFile;

                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    
                    result = this.FileSelectMode == FileSelectMode.MultiFile
                        ? string.Join(this.MultiFileSeparator, dialog.FileNames)
                        : dialog.FileName;
                }
            }

            this.Value = result;
        }
    }

    public enum FileSelectMode
    {
        SingleFile = 0,
        MultiFile = 1,
        Directory = 2,
    }
}
