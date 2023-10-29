namespace ValheimServerGUI.Forms
{
    partial class WorldPreferencesForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            ModifiersGroupBox = new System.Windows.Forms.GroupBox();
            ModifierPortalsFormField = new ValheimServerGUI.Controls.DropdownFormField();
            ModifierResourcesFormField = new ValheimServerGUI.Controls.DropdownFormField();
            ModifierRaidsFormField = new ValheimServerGUI.Controls.DropdownFormField();
            ModifierDeathPenaltyFormField = new ValheimServerGUI.Controls.DropdownFormField();
            ModifierCombatFormField = new ValheimServerGUI.Controls.DropdownFormField();
            KeysGroupBox = new System.Windows.Forms.GroupBox();
            KeyNoMapFormField = new ValheimServerGUI.Controls.CheckboxFormField();
            KeyPassiveMobsFormField = new ValheimServerGUI.Controls.CheckboxFormField();
            KeyPlayerEventsFormField = new ValheimServerGUI.Controls.CheckboxFormField();
            KeyNoBuildCostFormField = new ValheimServerGUI.Controls.CheckboxFormField();
            PresetFormField = new ValheimServerGUI.Controls.DropdownFormField();
            ButtonCancel = new System.Windows.Forms.Button();
            ButtonOK = new System.Windows.Forms.Button();
            ButtonDefaults = new System.Windows.Forms.Button();
            WikiLinkLabel = new System.Windows.Forms.LinkLabel();
            label1 = new System.Windows.Forms.Label();
            ModifiersGroupBox.SuspendLayout();
            KeysGroupBox.SuspendLayout();
            SuspendLayout();
            // 
            // ModifiersGroupBox
            // 
            ModifiersGroupBox.Controls.Add(ModifierPortalsFormField);
            ModifiersGroupBox.Controls.Add(ModifierResourcesFormField);
            ModifiersGroupBox.Controls.Add(ModifierRaidsFormField);
            ModifiersGroupBox.Controls.Add(ModifierDeathPenaltyFormField);
            ModifiersGroupBox.Controls.Add(ModifierCombatFormField);
            ModifiersGroupBox.Location = new System.Drawing.Point(12, 59);
            ModifiersGroupBox.Name = "ModifiersGroupBox";
            ModifiersGroupBox.Size = new System.Drawing.Size(200, 262);
            ModifiersGroupBox.TabIndex = 1;
            ModifiersGroupBox.TabStop = false;
            ModifiersGroupBox.Text = "Modifiers";
            // 
            // ModifierPortalsFormField
            // 
            ModifierPortalsFormField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ModifierPortalsFormField.DropdownEnabled = true;
            ModifierPortalsFormField.EmptyText = "";
            ModifierPortalsFormField.HelpText = "";
            ModifierPortalsFormField.LabelText = "Portals";
            ModifierPortalsFormField.Location = new System.Drawing.Point(6, 210);
            ModifierPortalsFormField.Name = "ModifierPortalsFormField";
            ModifierPortalsFormField.Size = new System.Drawing.Size(188, 41);
            ModifierPortalsFormField.TabIndex = 5;
            ModifierPortalsFormField.Value = null;
            // 
            // ModifierResourcesFormField
            // 
            ModifierResourcesFormField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ModifierResourcesFormField.DropdownEnabled = true;
            ModifierResourcesFormField.EmptyText = "";
            ModifierResourcesFormField.HelpText = "";
            ModifierResourcesFormField.LabelText = "Resources";
            ModifierResourcesFormField.Location = new System.Drawing.Point(6, 163);
            ModifierResourcesFormField.Name = "ModifierResourcesFormField";
            ModifierResourcesFormField.Size = new System.Drawing.Size(188, 41);
            ModifierResourcesFormField.TabIndex = 4;
            ModifierResourcesFormField.Value = null;
            // 
            // ModifierRaidsFormField
            // 
            ModifierRaidsFormField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ModifierRaidsFormField.DropdownEnabled = true;
            ModifierRaidsFormField.EmptyText = "";
            ModifierRaidsFormField.HelpText = "";
            ModifierRaidsFormField.LabelText = "Raids";
            ModifierRaidsFormField.Location = new System.Drawing.Point(6, 116);
            ModifierRaidsFormField.Name = "ModifierRaidsFormField";
            ModifierRaidsFormField.Size = new System.Drawing.Size(188, 41);
            ModifierRaidsFormField.TabIndex = 3;
            ModifierRaidsFormField.Value = null;
            // 
            // ModifierDeathPenaltyFormField
            // 
            ModifierDeathPenaltyFormField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ModifierDeathPenaltyFormField.DropdownEnabled = true;
            ModifierDeathPenaltyFormField.EmptyText = "";
            ModifierDeathPenaltyFormField.HelpText = "";
            ModifierDeathPenaltyFormField.LabelText = "Death Penalty";
            ModifierDeathPenaltyFormField.Location = new System.Drawing.Point(6, 69);
            ModifierDeathPenaltyFormField.Name = "ModifierDeathPenaltyFormField";
            ModifierDeathPenaltyFormField.Size = new System.Drawing.Size(188, 41);
            ModifierDeathPenaltyFormField.TabIndex = 1;
            ModifierDeathPenaltyFormField.Value = null;
            // 
            // ModifierCombatFormField
            // 
            ModifierCombatFormField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ModifierCombatFormField.DropdownEnabled = true;
            ModifierCombatFormField.EmptyText = "";
            ModifierCombatFormField.HelpText = "";
            ModifierCombatFormField.LabelText = "Combat";
            ModifierCombatFormField.Location = new System.Drawing.Point(6, 22);
            ModifierCombatFormField.Name = "ModifierCombatFormField";
            ModifierCombatFormField.Size = new System.Drawing.Size(188, 41);
            ModifierCombatFormField.TabIndex = 0;
            ModifierCombatFormField.Value = null;
            // 
            // KeysGroupBox
            // 
            KeysGroupBox.Controls.Add(KeyNoMapFormField);
            KeysGroupBox.Controls.Add(KeyPassiveMobsFormField);
            KeysGroupBox.Controls.Add(KeyPlayerEventsFormField);
            KeysGroupBox.Controls.Add(KeyNoBuildCostFormField);
            KeysGroupBox.Location = new System.Drawing.Point(222, 59);
            KeysGroupBox.Name = "KeysGroupBox";
            KeysGroupBox.Size = new System.Drawing.Size(200, 262);
            KeysGroupBox.TabIndex = 2;
            KeysGroupBox.TabStop = false;
            KeysGroupBox.Text = "Additional Keys";
            // 
            // KeyNoMapFormField
            // 
            KeyNoMapFormField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            KeyNoMapFormField.HelpText = "";
            KeyNoMapFormField.LabelText = "No Map";
            KeyNoMapFormField.Location = new System.Drawing.Point(6, 45);
            KeyNoMapFormField.Name = "KeyNoMapFormField";
            KeyNoMapFormField.Size = new System.Drawing.Size(188, 17);
            KeyNoMapFormField.TabIndex = 3;
            KeyNoMapFormField.Value = false;
            // 
            // KeyPassiveMobsFormField
            // 
            KeyPassiveMobsFormField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            KeyPassiveMobsFormField.HelpText = "";
            KeyPassiveMobsFormField.LabelText = "Passive Mobs";
            KeyPassiveMobsFormField.Location = new System.Drawing.Point(6, 68);
            KeyPassiveMobsFormField.Name = "KeyPassiveMobsFormField";
            KeyPassiveMobsFormField.Size = new System.Drawing.Size(188, 17);
            KeyPassiveMobsFormField.TabIndex = 2;
            KeyPassiveMobsFormField.Value = false;
            // 
            // KeyPlayerEventsFormField
            // 
            KeyPlayerEventsFormField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            KeyPlayerEventsFormField.HelpText = "";
            KeyPlayerEventsFormField.LabelText = "Player Events";
            KeyPlayerEventsFormField.Location = new System.Drawing.Point(6, 91);
            KeyPlayerEventsFormField.Name = "KeyPlayerEventsFormField";
            KeyPlayerEventsFormField.Size = new System.Drawing.Size(188, 17);
            KeyPlayerEventsFormField.TabIndex = 1;
            KeyPlayerEventsFormField.Value = false;
            // 
            // KeyNoBuildCostFormField
            // 
            KeyNoBuildCostFormField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            KeyNoBuildCostFormField.HelpText = "";
            KeyNoBuildCostFormField.LabelText = "No Build Cost";
            KeyNoBuildCostFormField.Location = new System.Drawing.Point(6, 22);
            KeyNoBuildCostFormField.Name = "KeyNoBuildCostFormField";
            KeyNoBuildCostFormField.Size = new System.Drawing.Size(188, 17);
            KeyNoBuildCostFormField.TabIndex = 0;
            KeyNoBuildCostFormField.Value = false;
            // 
            // PresetFormField
            // 
            PresetFormField.DropdownEnabled = true;
            PresetFormField.EmptyText = "";
            PresetFormField.HelpText = "";
            PresetFormField.LabelText = "Modifier Preset";
            PresetFormField.Location = new System.Drawing.Point(12, 12);
            PresetFormField.Name = "PresetFormField";
            PresetFormField.Size = new System.Drawing.Size(200, 41);
            PresetFormField.TabIndex = 0;
            PresetFormField.Value = null;
            // 
            // ButtonCancel
            // 
            ButtonCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            ButtonCancel.Location = new System.Drawing.Point(347, 342);
            ButtonCancel.Name = "ButtonCancel";
            ButtonCancel.Size = new System.Drawing.Size(75, 23);
            ButtonCancel.TabIndex = 5;
            ButtonCancel.Text = "Cancel";
            ButtonCancel.UseVisualStyleBackColor = true;
            ButtonCancel.Click += ButtonCancel_Click;
            // 
            // ButtonOK
            // 
            ButtonOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            ButtonOK.Location = new System.Drawing.Point(266, 342);
            ButtonOK.Name = "ButtonOK";
            ButtonOK.Size = new System.Drawing.Size(75, 23);
            ButtonOK.TabIndex = 4;
            ButtonOK.Text = "OK";
            ButtonOK.UseVisualStyleBackColor = true;
            ButtonOK.Click += ButtonOK_Click;
            // 
            // ButtonDefaults
            // 
            ButtonDefaults.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            ButtonDefaults.Location = new System.Drawing.Point(12, 342);
            ButtonDefaults.Name = "ButtonDefaults";
            ButtonDefaults.Size = new System.Drawing.Size(111, 23);
            ButtonDefaults.TabIndex = 3;
            ButtonDefaults.Text = "Restore Defaults";
            ButtonDefaults.UseVisualStyleBackColor = true;
            ButtonDefaults.Click += ButtonDefaults_Click;
            // 
            // WikiLinkLabel
            // 
            WikiLinkLabel.AutoSize = true;
            WikiLinkLabel.Location = new System.Drawing.Point(266, 27);
            WikiLinkLabel.Name = "WikiLinkLabel";
            WikiLinkLabel.Size = new System.Drawing.Size(78, 15);
            WikiLinkLabel.TabIndex = 6;
            WikiLinkLabel.TabStop = true;
            WikiLinkLabel.Text = "Valheim Wiki.";
            WikiLinkLabel.LinkClicked += WikiLinkLabel_LinkClicked;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(228, 12);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(184, 30);
            label1.TabIndex = 7;
            label1.Text = "Read more about world modifiers\r\non the";
            // 
            // WorldPreferencesForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(434, 377);
            Controls.Add(WikiLinkLabel);
            Controls.Add(ButtonDefaults);
            Controls.Add(ButtonOK);
            Controls.Add(ButtonCancel);
            Controls.Add(PresetFormField);
            Controls.Add(KeysGroupBox);
            Controls.Add(ModifiersGroupBox);
            Controls.Add(label1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new System.Drawing.Size(450, 321);
            Name = "WorldPreferencesForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "World Settings";
            ModifiersGroupBox.ResumeLayout(false);
            KeysGroupBox.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox ModifiersGroupBox;
        private ValheimServerGUI.Controls.DropdownFormField ModifierPortalsFormField;
        private ValheimServerGUI.Controls.DropdownFormField ModifierResourcesFormField;
        private ValheimServerGUI.Controls.DropdownFormField ModifierRaidsFormField;
        private ValheimServerGUI.Controls.DropdownFormField ModifierDeathPenaltyFormField;
        private ValheimServerGUI.Controls.DropdownFormField ModifierCombatFormField;
        private System.Windows.Forms.GroupBox KeysGroupBox;
        private ValheimServerGUI.Controls.DropdownFormField PresetFormField;
        private ValheimServerGUI.Controls.CheckboxFormField KeyNoMapFormField;
        private ValheimServerGUI.Controls.CheckboxFormField KeyPassiveMobsFormField;
        private ValheimServerGUI.Controls.CheckboxFormField KeyPlayerEventsFormField;
        private ValheimServerGUI.Controls.CheckboxFormField KeyNoBuildCostFormField;
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.Button ButtonOK;
        private System.Windows.Forms.Button ButtonDefaults;
        private System.Windows.Forms.LinkLabel WikiLinkLabel;
        private System.Windows.Forms.Label label1;
    }
}