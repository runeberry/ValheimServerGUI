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
            string message)
        {
            InitializeComponent();

            this.Text = title;
            this.TextInputField.LabelText = message;

            this.ButtonOK.Click += this.ButtonOK_Click;
            this.ButtonCancel.Click += this.ButtonCancel_Click;
        }

        public void SetValidation(string helpText, Func<string, bool> onValidate)
        {
            this.TextInputField.HelpText = helpText;
            this.OnValidate = onValidate;
        }

        #region Form events

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            if (!this.ValidateInput()) return;

            this.Value = this.TextInputField.Value;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        #endregion

        #region Non-public methods

        private bool ValidateInput()
        {
            var userInput = this.TextInputField.Value;

            string errMessage = null;
            try
            {
                if (!this.OnValidate(userInput))
                {
                    errMessage = this.TextInputField.HelpText ?? "The provided string is invalid.";
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
