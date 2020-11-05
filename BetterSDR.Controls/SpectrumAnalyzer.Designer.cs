namespace BetterSDR.Controls
{
    partial class SpectrumAnalyzer
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
            this.pbPlot = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbPlot)).BeginInit();
            this.SuspendLayout();
            // 
            // pbPlot
            // 
            this.pbPlot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbPlot.Location = new System.Drawing.Point(0, 0);
            this.pbPlot.Name = "pbPlot";
            this.pbPlot.Size = new System.Drawing.Size(284, 205);
            this.pbPlot.TabIndex = 0;
            this.pbPlot.TabStop = false;
            this.pbPlot.SizeChanged += new System.EventHandler(this.pbPlot_SizeChanged);
            this.pbPlot.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.pbPlot_MouseDoubleClick);
            this.pbPlot.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pbPlot_MouseDown);
            this.pbPlot.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pbPlot_MouseMove);
            this.pbPlot.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pbPlot_MouseUp);
            // 
            // SpectrumAnalyzer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.Controls.Add(this.pbPlot);
            this.DoubleBuffered = true;
            this.Name = "SpectrumAnalyzer";
            this.Size = new System.Drawing.Size(284, 205);
            ((System.ComponentModel.ISupportInitialize)(this.pbPlot)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbPlot;
    }
}
