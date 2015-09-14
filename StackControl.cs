using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace debugger
{
    public class StackControl : EmuMemoryControl
    {
        private static Pen _linePen = new Pen(Color.LightGray);
        private static Brush _addrBrush = new SolidBrush(Color.Gray);
        private static Brush _textBrush = new SolidBrush(Color.Black);
        private static Brush _nfTextBrush = new SolidBrush(Color.Gray);
        private static Brush _selBgBrush = new SolidBrush(Color.LightGray);
        private static Brush _curAddrBrush = new SolidBrush(Color.White);
        private static Brush _curAddrBgBrush = new SolidBrush(Color.Black);

        public StackControl() {
            AddressAlignment = 4;
            SizePerLine = 4;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            uint numVisLines = this.VisibleSize / this.SizePerLine;

            Graphics g = e.Graphics;

            g.Clear(Color.White);

            SizeF addrStrSize = g.MeasureString("FFFFFFFF", this.Font);
            SizeF dataStrSize = g.MeasureString("FFFFFFFF", this.Font);

            float addrX = 0;
            float div1X = addrX + addrStrSize.Width;
            float dataX = div1X + 4;
            float data0X = dataX;
            float div2X = dataX + dataStrSize.Width;

            Brush textBrush = _nfTextBrush;
            if (this.Focused)
            {
                textBrush = _textBrush;
            }

            if (this.DataView != null)
            {
                this.DataView.Seek(this.Address);

                uint curAddr = this.Address;
                float lineY = 0;
                for (var i = 0; i < numVisLines; ++i, curAddr += 4)
                {
                    float textY = lineY;

                    if (!this.DataView.Eof)
                    {

                        if (curAddr >= SelectedAddressStart && curAddr <= SelectedAddressEnd)
                        {
                            g.FillRectangle(_selBgBrush, 0, lineY, this.ClientSize.Width, this.Font.Height);
                        }

                        string addrStr = String.Format("{0:X8}", curAddr);
                        if (curAddr == this.ActiveAddress)
                        {
                            g.FillRectangle(_curAddrBgBrush, 0, lineY, div1X, this.Font.Height);
                            g.DrawString(addrStr, this.Font, _curAddrBrush, addrX, textY);
                        }
                        else
                        {
                            g.DrawString(addrStr, this.Font, _addrBrush, addrX, textY);
                        }

                        string valueStr = "????????";
                        uint stackVal;
                        if (this.DataView.GetUint32(out stackVal))
                        {
                            valueStr = String.Format("{0:X8}", stackVal);
                        }
                        g.DrawString(valueStr, this.Font, textBrush, dataX, textY);
                    } else
                    {
                        g.DrawString("????????" , this.Font, _addrBrush, addrX, textY);
                    }

                    lineY += this.Font.Height;
                }
            }

            g.DrawLine(_linePen, div1X, 0, div1X, this.ClientSize.Height);
            g.DrawLine(_linePen, div2X, 0, div2X, this.ClientSize.Height);
        }
    }
}
