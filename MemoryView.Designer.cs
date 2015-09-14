namespace debugger
{
    partial class MemoryView
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
            this.memDisp = new debugger.MemoryControl();
            this.SuspendLayout();
            // 
            // memDisp
            // 
            this.memDisp.ActiveAddress = ((uint)(0u));
            this.memDisp.Address = ((uint)(0u));
            this.memDisp.AddressAlignment = ((uint)(1u));
            this.memDisp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.memDisp.DataView = null;
            this.memDisp.DebugManager = null;
            this.memDisp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.memDisp.Location = new System.Drawing.Point(0, 0);
            this.memDisp.MinimumSize = new System.Drawing.Size(233, 85);
            this.memDisp.Name = "memDisp";
            this.memDisp.SelectedAddressEnd = ((uint)(0u));
            this.memDisp.SelectedAddressStart = ((uint)(0u));
            this.memDisp.Size = new System.Drawing.Size(615, 347);
            this.memDisp.SizePerLine = ((uint)(16u));
            this.memDisp.TabIndex = 0;
            // 
            // MemoryView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 11F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(615, 347);
            this.Controls.Add(this.memDisp);
            this.Font = new System.Drawing.Font("Lucida Console", 8.25F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "MemoryView";
            this.Text = "MemoryView";
            this.ResumeLayout(false);

        }

        #endregion

        private debugger.MemoryControl memDisp;
    }
}