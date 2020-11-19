namespace BetterSDR.Controls {
    partial class FrequencyEditDidgit {
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.SuspendLayout();
            // 
            // FrequencyEditDidgit
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.DoubleBuffered = true;
            this.Name = "FrequencyEditDidgit";
            this.Scroll += new System.Windows.Forms.ScrollEventHandler(this.FrequencyEditDidgit_Scroll);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.FrequencyEditDidgit_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.FrequencyEditDidgit_MouseDown);
            this.MouseEnter += new System.EventHandler(this.FrequencyEditDidgit_MouseEnter);
            this.MouseLeave += new System.EventHandler(this.FrequencyEditDidgit_MouseLeave);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.FrequencyEditDidgit_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.FrequencyEditDidgit_MouseUp);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
