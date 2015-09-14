using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace debugger
{
    public class MemoryControl : EmuMemoryControl
    {
        private static Pen _linePen = new Pen(Color.LightGray);
        private static Brush _addrBrush = new SolidBrush(Color.Gray);
        private static Brush _textBrush = new SolidBrush(Color.Black);
        private static Brush _nfTextBrush = new SolidBrush(Color.Gray);
        private static Brush _selBgBrush = new SolidBrush(Color.LightGray);

        private ContextMenuStrip contextMenu;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem goToAddressToolStripMenuItem;

        public MemoryControl()
        {
            InitializeComponent();

            AddressAlignment = 1;
            SizePerLine = 16;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            uint numVisLines = this.VisibleSize / this.SizePerLine;

            Graphics g = e.Graphics;

            g.Clear(Color.White);

            SizeF addrStrSize = g.MeasureString("FFFFFFFF", this.Font);
            SizeF dataStrSize = g.MeasureString("FF", this.Font);

            float dataWidth = dataStrSize.Width;
            float addrX = 0;
            float div1X = addrX + addrStrSize.Width;
            float dataX = div1X + 4;
            float div2X = dataX + dataWidth * SizePerLine;
            float asciiX = div2X + 4;

            Brush textBrush = _nfTextBrush;
            if (this.Focused)
            {
                textBrush = _textBrush;
            }

            if (this.DataView != null)
            {
                byte[] memVal = new byte[1];
                this.DataView.Seek(this.Address);

                uint curAddr = this.Address;
                float lineY = 0;
                for (var i = 0; i < numVisLines; ++i, curAddr += SizePerLine)
                {
                    float textY = lineY;

                    if (!this.DataView.Eof)
                    {
                        if (curAddr >= SelectedAddressStart && curAddr <= SelectedAddressEnd)
                        {
                            g.FillRectangle(_selBgBrush, 0, lineY, this.ClientSize.Width, this.Font.Height);
                        }

                        string addrStr = String.Format("{0:X8}", curAddr);
                        g.DrawString(addrStr, this.Font, _addrBrush, addrX, textY);

                        float curDataX = dataX;
                        string curAsciiStr = "";
                        for (var j = 0; j < SizePerLine; ++j)
                        {
                            string asciiStr = " ";

                            if (!this.DataView.Eof)
                            {
                                string valueStr = "??";

                                if (this.DataView.GetUint8(out memVal[0]))
                                {
                                    valueStr = String.Format("{0:X2}", memVal[0]);
                                    if (memVal[0] >= 32 && memVal[0] <= 126)
                                    {
                                        asciiStr = UTF8Encoding.ASCII.GetString(memVal);
                                    }
                                }

                                g.DrawString(valueStr, this.Font, textBrush, curDataX, textY);
                            }

                            curAsciiStr += asciiStr;
                            curDataX += dataWidth;
                        }

                        g.DrawString(curAsciiStr, this.Font, textBrush, asciiX, textY);
                    }
                    else
                    {
                        g.DrawString("????????", this.Font, _addrBrush, addrX, textY);
                    }

                    lineY += this.Font.Height;
                }
            }

            g.DrawLine(_linePen, div1X, 0, div1X, this.ClientSize.Height);
            g.DrawLine(_linePen, div2X, 0, div2X, this.ClientSize.Height);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.contextMenu.Show(this, e.Location);
            }
            base.OnMouseDown(e);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.goToAddressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.goToAddressToolStripMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(171, 48);
            // 
            // goToAddressToolStripMenuItem
            // 
            this.goToAddressToolStripMenuItem.Name = "goToAddressToolStripMenuItem";
            this.goToAddressToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            this.goToAddressToolStripMenuItem.Text = "Go To Address";
            this.goToAddressToolStripMenuItem.Click += new System.EventHandler(this.goToAddressToolStripMenuItem_Click);
            // 
            // AssemblyControl
            // 
            this.Name = "AssemblyControl";
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private void goToAddressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ToggleUserGoto();
        }
    }
}
