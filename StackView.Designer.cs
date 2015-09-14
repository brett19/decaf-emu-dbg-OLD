namespace debugger
{
    partial class StackView
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
            this.stackDisp = new debugger.StackControl();
            this.SuspendLayout();
            // 
            // stackDisp
            // 
            this.stackDisp.ActiveAddress = ((uint)(0u));
            this.stackDisp.Address = ((uint)(0u));
            this.stackDisp.AddressAlignment = ((uint)(4u));
            this.stackDisp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.stackDisp.DataView = null;
            this.stackDisp.DebugManager = null;
            this.stackDisp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stackDisp.Location = new System.Drawing.Point(0, 0);
            this.stackDisp.MinimumSize = new System.Drawing.Size(200, 100);
            this.stackDisp.Name = "stackDisp";
            this.stackDisp.SelectedAddressEnd = ((uint)(0u));
            this.stackDisp.SelectedAddressStart = ((uint)(0u));
            this.stackDisp.Size = new System.Drawing.Size(502, 339);
            this.stackDisp.SizePerLine = ((uint)(4u));
            this.stackDisp.TabIndex = 2;
            // 
            // StackView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 11F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(502, 339);
            this.Controls.Add(this.stackDisp);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "StackView";
            this.Text = "Stack";
            this.ResumeLayout(false);

        }

        #endregion
        private StackControl stackDisp;
    }
}