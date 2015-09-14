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
            this.assemblyDisp = new debugger.AssemblyControl();
            this.SuspendLayout();
            // 
            // assemblyDisp
            // 
            this.assemblyDisp.ActiveAddress = ((uint)(0u));
            this.assemblyDisp.Address = ((uint)(0u));
            this.assemblyDisp.AddressAlignment = ((uint)(4u));
            this.assemblyDisp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.assemblyDisp.DataView = null;
            this.assemblyDisp.DebugManager = null;
            this.assemblyDisp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.assemblyDisp.Location = new System.Drawing.Point(0, 0);
            this.assemblyDisp.MinimumSize = new System.Drawing.Size(200, 100);
            this.assemblyDisp.Name = "assemblyDisp";
            this.assemblyDisp.SelectedAddressEnd = ((uint)(0u));
            this.assemblyDisp.SelectedAddressStart = ((uint)(0u));
            this.assemblyDisp.Size = new System.Drawing.Size(709, 346);
            this.assemblyDisp.SizePerLine = ((uint)(4u));
            this.assemblyDisp.TabIndex = 0;
            // 
            // AssemblyView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 11F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(709, 346);
            this.Controls.Add(this.assemblyDisp);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "AssemblyView";
            this.Text = "Assembly";
            this.ResumeLayout(false);

        }

        #endregion
        private AssemblyControl assemblyDisp;
    }
}

