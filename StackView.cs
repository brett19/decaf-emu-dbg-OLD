using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace debugger
{
    public partial class StackView : Form
    {
        public StackView()
        {
            InitializeComponent();

            scrollBar.Enabled = false;
        }

        private float LineHeight()
        {
            return this.Font.Height;
        }

        private void DataAvailNotif()
        {
            this.Invalidate();
        }

        private void Stack_Paint(object sender, PaintEventArgs e)
        {
            Pen linePen = new Pen(Color.LightGray);
            Brush addrBrush = new SolidBrush(Color.Gray);
            Brush textBrush = new SolidBrush(Color.Black);
            Brush selAddrBgBrush = new SolidBrush(Color.Black);
            Brush selBgBrush = new SolidBrush(Color.LightGray);
            Brush selAddrBrush = new SolidBrush(Color.White);

            float lineHeight = LineHeight();
            int numVisLines = (this.ClientSize.Height / (int)lineHeight) + 1;
            uint startAddr = (uint)scrollBar.Value * 4;

            Graphics g = e.Graphics;

            g.Clear(Color.White);

            SizeF addrStrSize = g.MeasureString("FFFFFFFF", this.Font);
            SizeF dataStrSize = g.MeasureString("FFFFFFFF", this.Font);

            float addrX = 0;
            float div1X = addrX + addrStrSize.Width;
            float dataX = div1X + 4;
            float data0X = dataX;
            float div2X = dataX + dataStrSize.Width;

            if (scrollBar.Enabled)
            {
                var rdr = NetHandler.GetMemoryReader(startAddr, DataAvailNotif);

                uint curAddr = startAddr;
                float lineY = 0;
                string lineVal;
                for (var i = 0; i < numVisLines; ++i, curAddr += 4)
                {
                    lineVal = String.Format("{0:X8}", curAddr);

                    if (curAddr == currentAddress)
                    {
                        g.FillRectangle(selAddrBgBrush, 0, lineY, addrStrSize.Width, addrStrSize.Height);

                        g.DrawString(lineVal, this.Font, selAddrBrush, addrX, 1 + lineY);
                    }
                    else
                    {
                        g.DrawString(lineVal, this.Font, addrBrush, addrX, 1 + lineY);
                    }


                    uint stackVal;
                    if (rdr.GetUInt32(out stackVal))
                    {
                        lineVal = String.Format("{0:X8}", stackVal);
                    }
                    else
                    {
                        lineVal = "????????";
                    }

                    g.DrawString(lineVal, this.Font, textBrush, dataX, 1 + lineY);

                    lineY += LineHeight();
                }
            }

            g.DrawLine(linePen, div1X, 0, div1X, this.Height);
            g.DrawLine(linePen, div2X, 0, div2X, this.Height);
        }

        public void GoToAddress(uint address)
        {
            scrollBar.Value = (int)(address / 4);
        }

        private uint currentAddress = 0;

        public void UpdateData(DebugPauseInfo pauseInfo, DebugThreadInfo activeThread)
        {
            if (activeThread != null)
            {
                scrollBar.Minimum = (int)(activeThread.stackEnd / 4) - 1;
                scrollBar.Maximum = (int)(activeThread.stackStart / 4);
                scrollBar.Enabled = true;

                uint stackCurrent = activeThread.gpr[1];
                currentAddress = stackCurrent;
                GoToAddress(stackCurrent);
            } else
            {
                scrollBar.Enabled = false;
            }

            this.Invalidate();
        }

        private void scrollBar_ValueChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void StackView_Resize(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void StackView_MouseWheel(object sender, MouseEventArgs e)
        {
            // Really I should be passing the event to the scrollbar...
            if (e.Delta != 0)
            {
                int newValue = scrollBar.Value - (e.Delta / 120);
                if (newValue < scrollBar.Minimum)
                {
                    newValue = scrollBar.Minimum;
                }
                if (newValue > scrollBar.Maximum)
                {
                    newValue = scrollBar.Maximum;
                }
                scrollBar.Value = newValue;
            }
        }
    }
}
