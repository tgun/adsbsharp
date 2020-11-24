namespace BetterSDR {
    partial class MainForm {
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
            this.components = new System.ComponentModel.Container();
            this.startButton = new System.Windows.Forms.Button();
            this.iPlot = new ScottPlot.FormsPlot();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btnSettings = new System.Windows.Forms.Button();
            this.btnUDPToggle = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.frequencyEdit1 = new BetterSDR.Controls.FrequencyEdit();
            this.spectrumAnalyzer1 = new BetterSDR.Controls.SpectrumAnalyzer();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // startButton
            // 
            this.startButton.Font = new System.Drawing.Font("Arial", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.startButton.Location = new System.Drawing.Point(6, 25);
            this.startButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(34, 34);
            this.startButton.TabIndex = 1;
            this.startButton.Text = "►";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // iPlot
            // 
            this.iPlot.Location = new System.Drawing.Point(6, 67);
            this.iPlot.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.iPlot.Name = "iPlot";
            this.iPlot.Size = new System.Drawing.Size(738, 389);
            this.iPlot.TabIndex = 2;
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(2, 1, 0, 1);
            this.menuStrip1.Size = new System.Drawing.Size(1465, 24);
            this.menuStrip1.TabIndex = 12;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 22);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 22);
            this.toolsToolStripMenuItem.Text = "&Tools";
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 22);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 22);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // btnSettings
            // 
            this.btnSettings.Font = new System.Drawing.Font("Arial", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSettings.Location = new System.Drawing.Point(44, 26);
            this.btnSettings.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(34, 34);
            this.btnSettings.TabIndex = 13;
            this.btnSettings.Text = "⚙";
            this.btnSettings.UseVisualStyleBackColor = true;
            // 
            // btnUDPToggle
            // 
            this.btnUDPToggle.Location = new System.Drawing.Point(746, 678);
            this.btnUDPToggle.Name = "btnUDPToggle";
            this.btnUDPToggle.Size = new System.Drawing.Size(61, 23);
            this.btnUDPToggle.TabIndex = 14;
            this.btnUDPToggle.Text = "UDP";
            this.btnUDPToggle.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(813, 678);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(61, 23);
            this.button1.TabIndex = 15;
            this.button1.Text = "Rec";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(880, 678);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(61, 23);
            this.button2.TabIndex = 16;
            this.button2.Text = "Play";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(947, 678);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(66, 23);
            this.button3.TabIndex = 17;
            this.button3.Text = "...";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // frequencyEdit1
            // 
            this.frequencyEdit1.BackColor = System.Drawing.Color.Transparent;
            this.frequencyEdit1.EntryMode = BetterSDR.Controls.EntryMode.None;
            this.frequencyEdit1.EntryModeActive = false;
            this.frequencyEdit1.Frequency = ((long)(0));
            this.frequencyEdit1.Location = new System.Drawing.Point(81, 22);
            this.frequencyEdit1.Margin = new System.Windows.Forms.Padding(0);
            this.frequencyEdit1.Name = "frequencyEdit1";
            this.frequencyEdit1.Size = new System.Drawing.Size(360, 39);
            this.frequencyEdit1.StepSize = 0;
            this.frequencyEdit1.TabIndex = 18;
            this.frequencyEdit1.FrequencyUpdated += new BetterSDR.Controls.FrequencyUpdatedArgs(this.frequencyEdit1_FrequencyUpdated);
            // 
            // spectrumAnalyzer1
            // 
            this.spectrumAnalyzer1.Attack = 0.9D;
            this.spectrumAnalyzer1.AxisColor = System.Drawing.Color.DarkGray;
            this.spectrumAnalyzer1.BackColor = System.Drawing.Color.Black;
            this.spectrumAnalyzer1.BackgroundColor = System.Drawing.Color.Black;
            this.spectrumAnalyzer1.Decay = 0.3D;
            this.spectrumAnalyzer1.EnableFilter = false;
            this.spectrumAnalyzer1.FilterBandwidth = 0;
            this.spectrumAnalyzer1.FilterOffset = 0;
            this.spectrumAnalyzer1.GridlinesColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.spectrumAnalyzer1.LabelColor = System.Drawing.Color.Silver;
            this.spectrumAnalyzer1.Location = new System.Drawing.Point(758, 67);
            this.spectrumAnalyzer1.Margin = new System.Windows.Forms.Padding(10, 9, 10, 9);
            this.spectrumAnalyzer1.Name = "spectrumAnalyzer1";
            this.spectrumAnalyzer1.ShowMaxLine = false;
            this.spectrumAnalyzer1.Size = new System.Drawing.Size(678, 389);
            this.spectrumAnalyzer1.SpectrumColor = System.Drawing.Color.Aqua;
            this.spectrumAnalyzer1.SpectrumWidth = 0;
            this.spectrumAnalyzer1.StepSize = 0;
            this.spectrumAnalyzer1.TabIndex = 11;
            this.spectrumAnalyzer1.UseSmoothing = false;
            this.spectrumAnalyzer1.UseSnap = false;
            this.spectrumAnalyzer1.Zoom = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1465, 608);
            this.Controls.Add(this.frequencyEdit1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnUDPToggle);
            this.Controls.Add(this.btnSettings);
            this.Controls.Add(this.spectrumAnalyzer1);
            this.Controls.Add(this.iPlot);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button startButton;
        private ScottPlot.FormsPlot iPlot;
        private BetterSDR.Controls.SpectrumAnalyzer spectrumAnalyzer1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Button btnUDPToggle;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private Controls.FrequencyEdit frequencyEdit1;
    }
}

