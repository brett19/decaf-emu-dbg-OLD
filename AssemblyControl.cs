using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace debugger
{
    class AssemblyControl : EmuMemoryControl
    {
        class lineInfo
        {
            public uint address = 0xFFFFFFFF;
            public bool dataAvailable = false;
            public bool textAvailable = false;
            public uint data = 0;
            public string text = "";
            public string comment = "";
        }

        private static Brush _lineBrush = new SolidBrush(Color.FromArgb(255, 100, 100, 100));
        private static Pen _linePen = new Pen(Color.LightGray);
        private static Brush _addrBrush = new SolidBrush(Color.Gray);
        private static Brush _textBrush = new SolidBrush(Color.Black);
        private static Brush _nfTextBrush = new SolidBrush(Color.Gray);
        private static Brush _bpAddrBgBrush = new SolidBrush(Color.OrangeRed);
        private static Brush _bpAddrBrush = new SolidBrush(Color.Black);
        private static Brush _selBgBrush = new SolidBrush(Color.LightGray);
        private static Brush _curAddrBgBrush = new SolidBrush(Color.Black);
        private static Brush _curAddrBrush = new SolidBrush(Color.White);
        private static Brush _curBpAddrBrush = new SolidBrush(Color.Red);

        private ContextMenuStrip contextMenu;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem toggleBreakpointToolStripMenuItem;
        private ToolStripMenuItem goToAddressToolStripMenuItem;
        private lineInfo[] _lines = null;

        public AssemblyControl()
        {
            InitializeComponent();

            AddressAlignment = 4;
            SizePerLine = 4;
        }

        // TODO: This should probably work even for selected lines you can't see...
        private lineInfo getSelectedLineInfo()
        {
            if (_lines == null)
            {
                return null;
            }

            int lineIdx = (int)(((long)SelectedAddressEnd - (long)Address) / SizePerLine);
            if (lineIdx < 0 || lineIdx >= _lines.Length)
            {
                return null;
            }

            return _lines[lineIdx];
        }

        // TODO: Move this into some common parsing place
        private static uint parseAddress(string text)
        {
            // Search for @FFFFFFFF
            var atIdx = text.IndexOf('@');
            if (atIdx >= 0)
            {
                // Found an @ symbol
                if (text.Length >= atIdx + 1 + 8)
                {
                    // We have enough characters for an addreess
                    var jmpAddrStr = text.Substring(atIdx + 1, 8);
                    uint jmpAddr = 0;
                    try
                    {
                        jmpAddr = Convert.ToUInt32(jmpAddrStr, 16);
                    }
                    catch (Exception) { }

                    if (jmpAddr != 0)
                    {
                        // We successfully parsed a jump target
                        return jmpAddr;
                    }
                }
            }
            return 0;
        }

        protected override void OnRecache(EventArgs e)
        {
            if (this.DataView == null)
            {
                _lines = null;
                Invalidate();
                return;
            }

            uint numVisLines = this.VisibleSize / this.SizePerLine;

            if (_lines == null || _lines.Length != numVisLines)
            {
                _lines = new lineInfo[numVisLines];
            }

            this.DataView.Seek(this.Address);

            for (uint i = 0; i < _lines.Length; ++i)
            {
                if (this.DataView.Eof)
                {
                    _lines[i] = null;
                    continue;
                }

                ulong curAddress = (ulong)this.Address + (i * this.SizePerLine);

                if (_lines[i] == null)
                {
                    _lines[i] = new lineInfo();
                }

                _lines[i].address = (uint)curAddress;

                string text;
                bool dataAvail = this.DataView.GetInstruction(out _lines[i].data, out text);
                _lines[i].dataAvailable = dataAvail;
                _lines[i].textAvailable = dataAvail;

                if (dataAvail)
                {
                    string comment = "";
                    var spStringX = text.Split(new char[] { ';' }, 2);
                    if (spStringX.Length > 1)
                    {
                        comment = "; " + spStringX[1].Trim();
                    }
                    var spString = spStringX[0].Trim().Split(new char[] { ' ' }, 2);
                    if (spString.Length > 1)
                    {
                        var mne = spString[0];
                        while (mne.Length < 7)
                        {
                            mne = mne + " ";
                        }
                        text = mne + " " + spString[1];
                    }
                    else
                    {
                        text = spString[0];
                    }

                    uint targetAddr = parseAddress(text);
                    if (targetAddr != 0)
                    {
                        DebugSymbolInfo sym = this.DebugManager.FindSymbol(targetAddr);
                        if (sym != null)
                        {
                            var symMod = this.DebugManager.GetModule(sym.moduleIdx);
                            string symText = symMod.name + "." + sym.name;
                            comment = symText + " " + comment;
                        }
                    }

                    _lines[i].text = text;
                    _lines[i].comment = comment;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.Clear(Color.White);

            SizeF addrStrSize = g.MeasureString("FFFFFFFF", this.Font);
            SizeF statStrSize = g.MeasureString("∙∙∙", this.Font);
            SizeF dataStrSize = g.MeasureString("FF", this.Font);
            SizeF insStrSize = g.MeasureString("PSQ_ST R4, R4, R4, R4, R4, R4, R4, R4", this.Font);

            float dataStrWidth = (dataStrSize.Width * 4);

            float addrX = 0;
            float div1X = addrX + addrStrSize.Width;
            float statX = div1X + 2;
            float dataX = statX + statStrSize.Width;
            float data0X = dataX;
            float data1X = dataX + (dataStrSize.Width * 1);
            float data2X = dataX + (dataStrSize.Width * 2);
            float data3X = dataX + (dataStrSize.Width * 3);
            float div2X = dataX + 8 + dataStrWidth;
            float insX = div2X + 3;
            float div3X = insX + insStrSize.Width;
            float cmtX = div3X + 2;

            Brush textBrush = _nfTextBrush;
            if (this.Focused)
            {
                textBrush = _textBrush;
            }

            if (_lines != null)
            {
                float lineY = 0;

                for (int i = 0; i < _lines.Length; i++)
                {
                    var lineI = _lines[i];
                    float textY = lineY;

                    if (lineI != null)
                    {
                        if (lineI.address >= SelectedAddressStart && lineI.address <= SelectedAddressEnd)
                        {
                            g.FillRectangle(_selBgBrush, 0, lineY, this.ClientSize.Width, this.Font.Height);
                        }

                        string addrStr = String.Format("{0:X8}", lineI.address);
                        if (lineI.address == ActiveAddress)
                        {
                            g.FillRectangle(_curAddrBgBrush, 0, lineY, div1X, this.Font.Height);

                            if (this.DebugManager.HasBreakpoint(lineI.address))
                            {
                                g.DrawString(addrStr, this.Font, _curBpAddrBrush, addrX, textY);
                            }
                            else
                            {
                                g.DrawString(addrStr, this.Font, _curAddrBrush, addrX, textY);
                            }
                        }
                        else
                        {
                            if (this.DebugManager.HasBreakpoint(lineI.address))
                            {
                                g.FillRectangle(_bpAddrBgBrush, 0, lineY, div1X, this.Font.Height);
                                g.DrawString(addrStr, this.Font, _bpAddrBrush, addrX, textY);
                            }
                            else
                            {
                                g.DrawString(addrStr, this.Font, _addrBrush, addrX, textY);
                            }
                        }

                        string flowStr = String.Format(" ∙ ");
                        g.DrawString(flowStr, this.Font, textBrush, statX, textY);

                        if (lineI.dataAvailable)
                        {
                            string byte0Str = String.Format("{0:X2}", (lineI.data >> 0) & 0xFF);
                            string byte1Str = String.Format("{0:X2}", (lineI.data >> 8) & 0xFF);
                            string byte2Str = String.Format("{0:X2}", (lineI.data >> 16) & 0xFF);
                            string byte3Str = String.Format("{0:X2}", (lineI.data >> 24) & 0xFF);

                            g.DrawString(byte0Str, this.Font, textBrush, data0X, textY);
                            g.DrawString(byte1Str, this.Font, textBrush, data1X, textY);
                            g.DrawString(byte2Str, this.Font, textBrush, data2X, textY);
                            g.DrawString(byte3Str, this.Font, textBrush, data3X, textY);
                        }
                        else
                        {
                            g.DrawString("??", this.Font, textBrush, data0X, textY);
                            g.DrawString("??", this.Font, textBrush, data1X, textY);
                            g.DrawString("??", this.Font, textBrush, data2X, textY);
                            g.DrawString("??", this.Font, textBrush, data3X, textY);
                        }

                        if (lineI.textAvailable)
                        {
                            g.DrawString(lineI.text, this.Font, textBrush, insX, textY);
                            g.DrawString(lineI.comment, this.Font, textBrush, cmtX, textY);
                        }
                        else
                        {
                            g.DrawString("---", this.Font, textBrush, insX, textY);
                        }
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
            g.DrawLine(_linePen, div3X, 0, div3X, this.ClientSize.Height);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                this.DebugManager.ToggleBreakpoint(SelectedAddressEnd);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                var selLine = getSelectedLineInfo();
                if (selLine != null)
                {
                    uint jmpAddr = parseAddress(selLine.text);
                    if (jmpAddr != 0)
                    {
                        JumpToAddress(jmpAddr);
                    }
                }
                e.Handled = true;
            }

            if (!e.Handled)
            {
                base.OnKeyDown(e);
            }
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
            this.toggleBreakpointToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.goToAddressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toggleBreakpointToolStripMenuItem,
            this.goToAddressToolStripMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(171, 48);
            // 
            // toggleBreakpointToolStripMenuItem
            // 
            this.toggleBreakpointToolStripMenuItem.Name = "toggleBreakpointToolStripMenuItem";
            this.toggleBreakpointToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            this.toggleBreakpointToolStripMenuItem.Text = "Toggle Breakpoint";
            this.toggleBreakpointToolStripMenuItem.Click += new System.EventHandler(this.toggleBreakpointToolStripMenuItem_Click);
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

        private void toggleBreakpointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DebugManager.ToggleBreakpoint(SelectedAddressEnd);
        }

        private void goToAddressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ToggleUserGoto();
        }
    }
}
