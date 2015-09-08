using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace debugger
{
    public partial class AssemblyView : Form
    {
        public AssemblyView()
        {
            InitializeComponent();

            scrollBar.Enabled = false;
        }

        private void DataAvailNotif()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                recache();
            });
        }

        class LineInfo
        {
            public uint address = 0xFFFFFFFF;
            public bool dataAvailable = false;
            public bool textAvailable = false;
            public uint data = 0;
            public string text = "";
            public string comment = "";
        }

        private LineInfo[] lines = null;

        private float LineHeight()
        {
            return 13;
        }

        private void recache()
        {
            Debug.Assert(!this.InvokeRequired);

            if (!scrollBar.Enabled)
            {
                lines = null;
                this.Invalidate();
                return;
            }

            float lineHeight = LineHeight();
            int numVisLines = (this.ClientSize.Height / (int)lineHeight) + 1;
            uint startAddr = (uint)scrollBar.Value * 4;

            if (lines == null || lines.Length != numVisLines)
            {
                lines = new LineInfo[numVisLines];
            }

            for (uint i = 0; i < lines.Length; ++i)
            {
                if (lines[i] == null)
                {
                    lines[i] = new LineInfo();
                }

                lines[i].address = startAddr + (i * 4);
            }

            // Try to load the data
            {
                var rdr = NetHandler.GetMemoryReader(startAddr, DataAvailNotif);
                for (uint i = 0; i < lines.Length; ++i)
                {
                    if (rdr.GetUInt32(out lines[i].data))
                    {
                        lines[i].dataAvailable = true;
                    } else
                    {
                        lines[i].dataAvailable = false;
                    }
                }
            }

            // Try to load the instructions
            {
                var rdr = NetHandler.GetInstrReader(startAddr, DataAvailNotif);
                for (uint i = 0; i < lines.Length; ++i)
                {
                    string text = "";
                    string comment = "";
                    if (rdr.GetInstr(out text))
                    {
                        lines[i].textAvailable = true;

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
                            DebugSymbolInfo sym = findSymbol(targetAddr);
                            if (sym != null)
                            {
                                var symMod = PauseInfo.modules[sym.moduleIdx];
                                var modName = symMod.name.Substring(0, symMod.name.Length - 4);
                                string symText = modName.ToUpper() + "." + sym.name;
                                comment = symText + " " + comment;
                            }
                        }
                    }
                    else
                    {
                        lines[i].textAvailable = false;
                    }
                    lines[i].text = text;
                    lines[i].comment = comment;
                }
            }

            Debug.WriteLine("AssemblyView: {0:x08}, {1:x08}", lines[0].address, lines[lines.Length-1].address);

            this.Invalidate();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Brush lineBrush = new SolidBrush(Color.FromArgb(255, 100, 100, 100));
            Brush addrBrush = new SolidBrush(Color.Gray);
            Brush textBrush = new SolidBrush(Color.Black);
            Brush bpAddrBrush = new SolidBrush(Color.Black);
            Brush bpAddrBgBrush = new SolidBrush(Color.OrangeRed);
            Brush selAddrBgBrush = new SolidBrush(Color.Black);
            Brush selBgBrush = new SolidBrush(Color.LightGray);
            Brush selAddrBrush = new SolidBrush(Color.White);
            Brush bpSelAddrBrush = new SolidBrush(Color.Red);
            Pen linePen = new Pen(Color.LightGray);

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
            float div2X = dataX + 3 + dataStrWidth;
            float insX = div2X + 3;
            float div3X = insX + insStrSize.Width;
            float cmtX = div3X + 2;

            if (lines != null)
            {
                float lineY = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    var lineI = lines[i];

                    string lineVal = String.Format("{0:X8}", lineI.address);

                    if (lineI.address == selectedAddress)
                    {
                        g.FillRectangle(selBgBrush, 0, lineY, this.Width, addrStrSize.Height);
                    }

                    if (lineI.address == currentAddress)
                    {
                        g.FillRectangle(selAddrBgBrush, 0, lineY, addrStrSize.Width, addrStrSize.Height);

                        if (mBreakpoints.Contains(lineI.address))
                        {
                            g.DrawString(lineVal, this.Font, bpSelAddrBrush, addrX, 1 + lineY);
                        }
                        else
                        {
                            g.DrawString(lineVal, this.Font, selAddrBrush, addrX, 1 + lineY);
                        }
                    }
                    else
                    {
                        if (mBreakpoints.Contains(lineI.address))
                        {
                            g.FillRectangle(bpAddrBgBrush, 0, lineY, addrStrSize.Width, addrStrSize.Height);
                            g.DrawString(lineVal, this.Font, bpAddrBrush, addrX, 1 + lineY);
                        }
                        else
                        {
                            g.DrawString(lineVal, this.Font, addrBrush, addrX, 1 + lineY);
                        }
                    }

                    lineVal = String.Format(" ∙ ");
                    g.DrawString(lineVal, this.Font, textBrush, statX, 1 + lineY);

                    if (lineI.dataAvailable)
                    {
                        lineVal = String.Format("{0:X2}", (lineI.data >> 0) & 0xFF);
                        g.DrawString(lineVal, this.Font, textBrush, data0X, 1 + lineY);
                        lineVal = String.Format("{0:X2}", (lineI.data >> 8) & 0xFF);
                        g.DrawString(lineVal, this.Font, textBrush, data1X, 1 + lineY);
                        lineVal = String.Format("{0:X2}", (lineI.data >> 16) & 0xFF);
                        g.DrawString(lineVal, this.Font, textBrush, data2X, 1 + lineY);
                        lineVal = String.Format("{0:X2}", (lineI.data >> 24) & 0xFF);
                        g.DrawString(lineVal, this.Font, textBrush, data3X, 1 + lineY);
                    }
                    else
                    {
                        g.DrawString("??", this.Font, textBrush, data0X, 1 + lineY);
                        g.DrawString("??", this.Font, textBrush, data1X, 1 + lineY);
                        g.DrawString("??", this.Font, textBrush, data2X, 1 + lineY);
                        g.DrawString("??", this.Font, textBrush, data3X, 1 + lineY);
                    }

                    if (lineI.textAvailable)
                    {
                        g.DrawString(lineI.text, this.Font, textBrush, insX, 1 + lineY);
                        g.DrawString(lineI.comment, this.Font, textBrush, cmtX, 1 + lineY);
                    }
                    else
                    {
                        g.DrawString("---", this.Font, textBrush, insX, 1 + lineY);
                    }

                    lineY += LineHeight();
                }
            }

            g.DrawLine(linePen, div1X, 0, div1X, this.Height);
            g.DrawLine(linePen, div2X, 0, div2X, this.Height);
            g.DrawLine(linePen, div3X, 0, div3X, this.Height);
        }

        public List<uint> mBreakpoints = new List<uint>();

        private void AssemblyView_MouseWheel(object sender, MouseEventArgs e)
        {
            // Really I should be passing the event to the scrollbar...
            if (e.Delta != 0)
            {
                int newValue = scrollBar.Value - (e.Delta / 120);
                if (newValue < scrollBar.Minimum) {
                    newValue = scrollBar.Minimum;
                }
                if (newValue > scrollBar.Maximum) {
                    newValue = scrollBar.Maximum;
                }
                scrollBar.Value = newValue;
            }
        }

        private void scrollBar_ValueChanged(object sender, EventArgs e)
        {
            recache();
        }

        private void AssemblyView_Resize(object sender, EventArgs e)
        {
            recache();
        }

        public void MoveToAddress(uint address)
        {
            selectedAddress = address;

            const int DEAD_SPACE = 5;
            uint curStart = 0; 
            uint curEnd = 0;

            if (lines != null)
            {
                curStart = (uint)scrollBar.Value * 4;
                curEnd = curStart + (uint)lines.Length * 4;
            }

            if (curStart == curEnd || address < curStart || address > curEnd)
            {
                scrollBar.Value = (int)(address / 4) - DEAD_SPACE;
            }
            else if (address > curEnd - DEAD_SPACE * 4)
            {
                scrollBar.Value = (int)(address / 4) - lines.Length + DEAD_SPACE;
            } else if (address < curStart + DEAD_SPACE * 4)
            {
                scrollBar.Value = (int)(address / 4) - DEAD_SPACE;
            } else
            {
                // Already on-screen
                this.Invalidate();
            }
        }

        public void UpdateData(DebugPauseInfo pauseInfo, DebugThreadInfo activeThread)
        {
            PauseInfo = pauseInfo;
            if (pauseInfo != null)
            {
                scrollBar.Enabled = true;
                scrollBar.Minimum = 0x00000000;
                scrollBar.Maximum = 0x40000000;

                currentAddress = activeThread.cia;
                MoveToAddress(currentAddress);
            } else
            {
                scrollBar.Enabled = false;
            }

            recache();
        }

        private DebugSymbolInfo findSymbol(uint address)
        {
            var symbols = PauseInfo.symbols;

            if (symbols == null)
            {
                return null;
            }

            for(var i = 0; i < symbols.Length; ++i)
            {
                if (symbols[i].address == address)
                {
                    return symbols[i];
                }
            }
            return null;
        }

        private DebugPauseInfo PauseInfo;
        private uint currentAddress = 0;
        private uint selectedAddress = 0;

        private void AssemblyView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.G)
            {
                gotoBox.Visible = !gotoBox.Visible;
                if (gotoBox.Visible)
                {
                    gotoBox.Text = "";
                    gotoBox.Focus();
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyCode == Keys.F2)
            {
                ToggleBreakpoint();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                // Follow Jump
                LineInfo selLine = GetSelLineInfo();
                if (selLine != null)
                {
                    uint jmpAddr = parseAddress(selLine.text);
                    if (jmpAddr != 0)
                    {
                        MoveToAddress(jmpAddr);
                    }
                }
            }
        }

        private void gotoBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.G)
            {
                AssemblyView_KeyUp(sender, e);
            }
             else if (e.KeyCode == Keys.Enter)
            {
                uint addr = currentAddress;
                try
                {
                    if (gotoBox.Text.Trim().Length == 8)
                    {
                        addr = Convert.ToUInt32(gotoBox.Text.Trim(), 16);
                    }
                }
                catch (Exception) { }
                if (addr != currentAddress)
                {
                    MoveToAddress(addr);
                }
                gotoBox.Visible = false;
            }
        }

        private void AssemblyView_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private LineInfo GetSelLineInfo()
        {
            if (lines == null)
            {
                return null;
            }

            int selectedIndex = (int)(selectedAddress - lines[0].address) / 4;
            if (selectedIndex >= 0 && selectedIndex < lines.Length)
            {
                LineInfo selLine = lines[selectedIndex];
                return selLine;
            }
            return null;
        }

        private void ToggleBreakpoint()
        {
            LineInfo selLine = GetSelLineInfo();
            if (selLine != null)
            {
                // Toggle Breakpoint...
                if (mBreakpoints.Remove(selLine.address))
                {
                    NetHandler.SendRemoveBreakpoint(selLine.address);
                }
                else
                {
                    NetHandler.SendAddBreakpoint(selLine.address, 0);
                    mBreakpoints.Add(selLine.address);
                }
                recache();
            }
        }

        private void AssemblyView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ToggleBreakpoint();
        }

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

        private void AssemblyView_MouseDown(object sender, MouseEventArgs e)
        {
            if (lines != null)
            {
                uint lineIdx = (uint)(e.Y / LineHeight());
                selectedAddress = lines[0].address + lineIdx * 4;
                this.Invalidate();
            }
        }
    }
}
