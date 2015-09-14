namespace debugger
{
    partial class EmuMemoryControl
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
            this.scrollBar = new debugger.WheelVScrollBar();
            this.gotoPanel = new System.Windows.Forms.Panel();
            this.gotoLbl = new System.Windows.Forms.Label();
            this.gotoTxt = new System.Windows.Forms.TextBox();
            this.gotoPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // scrollBar
            // 
            this.scrollBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scrollBar.LargeChange = 1;
            this.scrollBar.Location = new System.Drawing.Point(404, 0);
            this.scrollBar.Name = "scrollBar";
            this.scrollBar.Size = new System.Drawing.Size(17, 232);
            this.scrollBar.TabIndex = 0;
            this.scrollBar.ValueChanged += new System.EventHandler(this.scrollBar_ValueChanged);
            this.scrollBar.KeyDown += new System.Windows.Forms.KeyEventHandler(this.scrollBar_KeyDown);
            // 
            // gotoPanel
            // 
            this.gotoPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.gotoPanel.Controls.Add(this.gotoTxt);
            this.gotoPanel.Controls.Add(this.gotoLbl);
            this.gotoPanel.Location = new System.Drawing.Point(147, 88);
            this.gotoPanel.Name = "gotoPanel";
            this.gotoPanel.Size = new System.Drawing.Size(110, 42);
            this.gotoPanel.TabIndex = 1;
            this.gotoPanel.Visible = false;
            // 
            // gotoLbl
            // 
            this.gotoLbl.AutoSize = true;
            this.gotoLbl.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gotoLbl.Location = new System.Drawing.Point(3, 5);
            this.gotoLbl.Name = "gotoLbl";
            this.gotoLbl.Size = new System.Drawing.Size(103, 11);
            this.gotoLbl.TabIndex = 0;
            this.gotoLbl.Text = "Go To Address:";
            // 
            // gotoTxt
            // 
            this.gotoTxt.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gotoTxt.Location = new System.Drawing.Point(24, 19);
            this.gotoTxt.MaxLength = 8;
            this.gotoTxt.Name = "gotoTxt";
            this.gotoTxt.Size = new System.Drawing.Size(64, 18);
            this.gotoTxt.TabIndex = 1;
            this.gotoTxt.Text = "00000000";
            this.gotoTxt.KeyDown += new System.Windows.Forms.KeyEventHandler(this.gotoTxt_KeyDown);
            // 
            // EmuMemoryControl
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.Controls.Add(this.gotoPanel);
            this.Controls.Add(this.scrollBar);
            this.DoubleBuffered = true;
            this.MinimumSize = new System.Drawing.Size(200, 100);
            this.Name = "EmuMemoryControl";
            this.Size = new System.Drawing.Size(421, 232);
            this.Enter += new System.EventHandler(this.EmuMemoryControl_Enter);
            this.gotoPanel.ResumeLayout(false);
            this.gotoPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private WheelVScrollBar scrollBar;
        private System.Windows.Forms.Panel gotoPanel;
        private System.Windows.Forms.TextBox gotoTxt;
        private System.Windows.Forms.Label gotoLbl;
    }
}
