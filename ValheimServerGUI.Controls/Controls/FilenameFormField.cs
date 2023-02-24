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

        #region IFormField implementation

        public string LabelText
        {
            get => Label.Text;
            set => Label.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        public string HelpText
        {
            get => HelpLabel.Text;
            set => HelpLabel.Text = value;
        }

        public string Value
        {
            get => TextBox.Text;
            set
            {
                if (TextBox.Text == value) return;

                TextBox.Text = value;
                ValueChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<string> ValueChanged;

        #endregion

        public bool ReadOnly
        {
            get => TextBox.ReadOnly;
            set
            {
                TextBox.ReadOnly = value;
                FileBrowserButton.Enabled = !value;
            }
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

            TextBox.TextChanged += OnTextChanged;
        }

        public void ConfigureFileDialog(Action<OpenFileDialog> builder)
        {
            FileDialogBuilder = builder;
        }

        public void ConfigureFolderDialog(Action<FolderBrowserDialog> builder)
        {
            FolderDialogBuilder = builder;
        }

        private void OnTextChanged(object sender, EventArgs args)
        {
            ValueChanged?.Invoke(this, Value);
        }

        private void FileBrowserButton_Click(object sender, EventArgs e)
        {
            string result;

            if (FileSelectMode == FileSelectMode.Directory)
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    if (FolderDialogBuilder != null)
                    {
                        FolderDialogBuilder(dialog);
                    }

                    dialog.RootFolder = Environment.SpecialFolder.ProgramFilesX86;

                    if (!string.IsNullOrWhiteSpace(InitialPath))
                    {
                        dialog.SelectedPath = new DirectoryInfo(InitialPath).FullName;
                    }
                    else if (!string.IsNullOrWhiteSpace(Value))
                    {
                        dialog.SelectedPath = new DirectoryInfo(Value).FullName;
                    }

                    if (dialog.ShowDialog() != DialogResult.OK) return;

                    result = dialog.SelectedPath;
                }
            }
            else
            {
                using var dialog = new OpenFileDialog();
                FileDialogBuilder?.Invoke(dialog);

                if (!string.IsNullOrWhiteSpace(InitialPath))
                {
                    dialog.InitialDirectory = new FileInfo(InitialPath).Directory.FullName;
                }
                else if (!string.IsNullOrWhiteSpace(Value))
                {
                    dialog.InitialDirectory = new FileInfo(Value).Directory.FullName;
                }

                // Override the Multiselect property based on this control's FileSelectMode
                dialog.Multiselect = FileSelectMode == FileSelectMode.MultiFile;

                if (dialog.ShowDialog() != DialogResult.OK) return;

                result = FileSelectMode == FileSelectMode.MultiFile
                    ? string.Join(MultiFileSeparator, dialog.FileNames)
                    : dialog.FileName;
            }

            Value = result;
        }
    }

    public enum FileSelectMode
    {
        SingleFile = 0,
        MultiFile = 1,
        Directory = 2,
    }
}
