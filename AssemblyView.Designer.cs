namespace debugger
{
    partial class AssemblyView
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
            this.scrollBar = new System.Windows.Forms.VScrollBar();
            this.gotoBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // scrollBar
            // 
            this.scrollBar.Dock = System.Windows.Forms.DockStyle.Right;
            this.scrollBar.Location = new System.Drawing.Point(692, 0);
            this.scrollBar.Name = "scrollBar";
            this.scrollBar.Size = new System.Drawing.Size(17, 346);
            this.scrollBar.TabIndex = 0;
            this.scrollBar.ValueChanged += new System.EventHandler(this.scrollBar_ValueChanged);
            // 
            // gotoBox
            // 
            this.gotoBox.Location = new System.Drawing.Point(0, 0);
            this.gotoBox.MaxLength = 8;
            this.gotoBox.Name = "gotoBox";
            this.gotoBox.Size = new System.Drawing.Size(100, 18);
            this.gotoBox.TabIndex = 1;
            this.gotoBox.Visible = false;
            this.gotoBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.gotoBox_KeyDown);
            // 
            // AssemblyView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 11F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(709, 346);
            this.Controls.Add(this.gotoBox);
            this.Controls.Add(this.scrollBar);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "AssemblyView";
            this.Text = "Assembly";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox1_Paint);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.AssemblyView_KeyUp);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.AssemblyView_MouseClick);
            this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.AssemblyView_MouseDoubleClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.AssemblyView_MouseDown);
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseWheel);
            this.Resize += new System.EventHandler(this.AssemblyView_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.VScrollBar scrollBar;
        private System.Windows.Forms.TextBox gotoBox;
    }
}

