using System;
using System.Windows.Forms;

namespace ValheimServerGUI.Forms
{
    public partial class TextPromptPopout : Form
    {
        public string Value { get; set; }

        private Func<string, bool> OnValidate;

        public TextPromptPopout(
            string title,
            string message,
            string startingText = null)
        {
            InitializeComponent();

            Text = title;
            TextInputField.LabelText = message;

            if (!string.IsNullOrWhiteSpace(startingText))
            {
                TextInputField.Value = startingText;
                TextInputField.SelectAll();
            }

            ButtonOK.Click += ButtonOK_Click;
            ButtonCancel.Click += ButtonCancel_Click;
        }

        public void SetValidation(string helpText, Func<string, bool> onValidate)
        {
            TextInputField.HelpText = helpText;
            OnValidate = onValidate;
        }

        #region Form events

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            Value = TextInputField.Value;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        #endregion

        #region Non-public methods

        private bool ValidateInput()
        {
            var userInput = TextInputField.Value;

            string errMessage = null;
            try
            {
                if (!OnValidate(userInput))
                {
                    errMessage = TextInputField.HelpText ?? "The provided string is invalid.";
                }
            }
            catch (Exception ex)
            {
                errMessage = ex.Message;
            }

            if (errMessage != null)
            {
                MessageBox.Show(
                    errMessage,
                    "Invalid Input",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }

            return true;
        }

        #endregion
    }
}
