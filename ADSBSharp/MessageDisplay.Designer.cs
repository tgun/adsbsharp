namespace ADSBSharp {
    partial class MessageDisplay {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.txtOldDecode = new System.Windows.Forms.TextBox();
            this.txtNewDecode = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // txtOldDecode
            // 
            this.txtOldDecode.Location = new System.Drawing.Point(0, 12);
            this.txtOldDecode.Multiline = true;
            this.txtOldDecode.Name = "txtOldDecode";
            this.txtOldDecode.Size = new System.Drawing.Size(788, 189);
            this.txtOldDecode.TabIndex = 0;
            // 
            // txtNewDecode
            // 
            this.txtNewDecode.Location = new System.Drawing.Point(0, 249);
            this.txtNewDecode.Multiline = true;
            this.txtNewDecode.Name = "txtNewDecode";
            this.txtNewDecode.Size = new System.Drawing.Size(788, 189);
            this.txtNewDecode.TabIndex = 1;
            // 
            // MessageDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.txtNewDecode);
            this.Controls.Add(this.txtOldDecode);
            this.Name = "MessageDisplay";
            this.Text = "MessageDisplay";
            this.Load += new System.EventHandler(this.MessageDisplay_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtOldDecode;
        private System.Windows.Forms.TextBox txtNewDecode;
    }
}