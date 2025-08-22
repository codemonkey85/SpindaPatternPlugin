namespace SpindaPatternPlugin.GUI
{
    partial class SpindaPatternForm
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
            if (disposing)
            {
                components?.Dispose();
                baseSprite?.Dispose();
                shinySprite?.Dispose();
                headMask?.Dispose();
                faceOverlay?.Dispose();
                mouthOverlay?.Dispose();
                pictureBox.Image?.Dispose();
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
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.patternLabel = new System.Windows.Forms.Label();
            this.patternInput = new System.Windows.Forms.TextBox();
            this.shinyCheckbox = new System.Windows.Forms.CheckBox();
            this.randomizeButton = new System.Windows.Forms.Button();
            this.applyButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox
            // 
            this.pictureBox.BackColor = System.Drawing.Color.White;
            this.pictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox.Location = new System.Drawing.Point(50, 20);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(300, 300);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            // 
            // patternLabel
            // 
            this.patternLabel.Location = new System.Drawing.Point(50, 340);
            this.patternLabel.Name = "patternLabel";
            this.patternLabel.Size = new System.Drawing.Size(120, 23);
            this.patternLabel.TabIndex = 1;
            this.patternLabel.Text = "Pattern Value:";
            this.patternLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // patternInput
            // 
            this.patternInput.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.patternInput.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.patternInput.Location = new System.Drawing.Point(175, 340);
            this.patternInput.MaxLength = 8;
            this.patternInput.Name = "patternInput";
            this.patternInput.Size = new System.Drawing.Size(100, 23);
            this.patternInput.TabIndex = 2;
            this.patternInput.TextChanged += new System.EventHandler(this.OnPatternChanged);
            // 
            // shinyCheckbox
            // 
            this.shinyCheckbox.AutoSize = true;
            this.shinyCheckbox.Location = new System.Drawing.Point(285, 342);
            this.shinyCheckbox.Name = "shinyCheckbox";
            this.shinyCheckbox.Size = new System.Drawing.Size(65, 21);
            this.shinyCheckbox.TabIndex = 3;
            this.shinyCheckbox.Text = "Shiny";
            this.shinyCheckbox.UseVisualStyleBackColor = true;
            this.shinyCheckbox.CheckedChanged += new System.EventHandler(this.OnShinyToggled);
            // 
            // randomizeButton
            // 
            this.randomizeButton.Location = new System.Drawing.Point(50, 380);
            this.randomizeButton.Name = "randomizeButton";
            this.randomizeButton.Size = new System.Drawing.Size(100, 30);
            this.randomizeButton.TabIndex = 4;
            this.randomizeButton.Text = "Randomize";
            this.randomizeButton.UseVisualStyleBackColor = true;
            this.randomizeButton.Click += new System.EventHandler(this.OnRandomize);
            // 
            // applyButton
            // 
            this.applyButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.applyButton.Location = new System.Drawing.Point(170, 430);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(75, 30);
            this.applyButton.TabIndex = 5;
            this.applyButton.Text = "Apply";
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.OnApply);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(255, 430);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 30);
            this.cancelButton.TabIndex = 6;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // SpindaPatternForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 500);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.randomizeButton);
            this.Controls.Add(this.shinyCheckbox);
            this.Controls.Add(this.patternInput);
            this.Controls.Add(this.patternLabel);
            this.Controls.Add(this.pictureBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SpindaPatternForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Spinda Pattern Editor";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Label patternLabel;
        private System.Windows.Forms.TextBox patternInput;
        private System.Windows.Forms.CheckBox shinyCheckbox;
        private System.Windows.Forms.Button randomizeButton;
        private System.Windows.Forms.Button applyButton;
        private System.Windows.Forms.Button cancelButton;
    }
}