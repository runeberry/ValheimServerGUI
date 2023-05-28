namespace ValheimServerGUI.Forms
{
    partial class AddRemoveListField
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            SelectListField = new ValheimServerGUI.Controls.SelectListField();
            AddButton = new System.Windows.Forms.Button();
            RemoveButton = new System.Windows.Forms.Button();
            EditButton = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // SelectListField
            // 
            SelectListField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            SelectListField.HelpText = "";
            SelectListField.LabelText = "Label";
            SelectListField.Location = new System.Drawing.Point(0, 0);
            SelectListField.Name = "SelectListField";
            SelectListField.Size = new System.Drawing.Size(150, 104);
            SelectListField.TabIndex = 0;
            SelectListField.Value = null;
            // 
            // AddButton
            // 
            AddButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            AddButton.Image = Properties.Resources.Add_16x;
            AddButton.Location = new System.Drawing.Point(124, 103);
            AddButton.Name = "AddButton";
            AddButton.Size = new System.Drawing.Size(23, 23);
            AddButton.TabIndex = 3;
            AddButton.UseVisualStyleBackColor = true;
            // 
            // RemoveButton
            // 
            RemoveButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            RemoveButton.Image = Properties.Resources.Remove_16x;
            RemoveButton.Location = new System.Drawing.Point(98, 103);
            RemoveButton.Name = "RemoveButton";
            RemoveButton.Size = new System.Drawing.Size(23, 23);
            RemoveButton.TabIndex = 2;
            RemoveButton.UseVisualStyleBackColor = true;
            // 
            // EditButton
            // 
            EditButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            EditButton.Image = Properties.Resources.Edit_16x;
            EditButton.Location = new System.Drawing.Point(3, 103);
            EditButton.Name = "EditButton";
            EditButton.Size = new System.Drawing.Size(23, 23);
            EditButton.TabIndex = 1;
            EditButton.UseVisualStyleBackColor = true;
            // 
            // AddRemoveSelectList
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(EditButton);
            Controls.Add(RemoveButton);
            Controls.Add(AddButton);
            Controls.Add(SelectListField);
            Name = "AddRemoveSelectList";
            Size = new System.Drawing.Size(150, 129);
            ResumeLayout(false);
        }

        #endregion

        private ValheimServerGUI.Controls.SelectListField SelectListField;
        private System.Windows.Forms.Button AddButton;
        private System.Windows.Forms.Button RemoveButton;
        private System.Windows.Forms.Button EditButton;
    }
}
