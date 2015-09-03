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
    public partial class RegisterView : Form
    {
        public RegisterView()
        {
            InitializeComponent();
        }

        private void RegisterView_Load(object sender, EventArgs e)
        {

        }

        private DebugThreadInfo info = null;
        private DebugThreadInfo oldInfo = null;

        public void UpdateData(DebugThreadInfo tinfo)
        {
            oldInfo = info;
            info = tinfo;
            this.Invalidate();
        }

        private float GetLineHeight()
        {
            return 13;
        }

        private void RegisterView_Paint(object sender, PaintEventArgs e)
        {
            Brush textBrush = new SolidBrush(Color.Black);
            Brush textChangedBrush = new SolidBrush(Color.Red);

            Graphics g = e.Graphics;

            g.Clear(Color.White);

            float lineY = 2;
            float col1X = 0;
            float col2X = 140;

            float col1NameX = col1X;
            float col1ValueX = col1NameX + 30;
            float col2NameX = col2X;
            float col2ValueX = col2NameX + 30;

            string lineStr;
            bool valueChanged;

            // GPRs
            for (int i = 0; i < 16; ++i)
            {
                lineStr = String.Format("R{0:d2}", i);
                g.DrawString(lineStr, this.Font, textBrush, col1NameX, lineY);

                valueChanged = false;
                if (info != null)
                {
                    lineStr = String.Format("{0:X8}", info.gpr[i]);
                    if (oldInfo != null)
                    {
                        valueChanged = info.gpr[i] != oldInfo.gpr[i];
                    }
                } else
                {
                    lineStr = "????????";
                }
                if (valueChanged)
                {
                    g.DrawString(lineStr, this.Font, textChangedBrush, col1ValueX, lineY);
                } else
                {
                    g.DrawString(lineStr, this.Font, textBrush, col1ValueX, lineY);
                }



                lineStr = String.Format("R{0:d2}", 16 + i);
                g.DrawString(lineStr, this.Font, textBrush, col2NameX, lineY);

                valueChanged = false;
                if (info != null)
                {
                    lineStr = String.Format("{0:X8}", info.gpr[16 + i]);
                    if (oldInfo != null)
                    {
                        valueChanged = info.gpr[16 + i] != oldInfo.gpr[16 + i];
                    }
                }
                else
                {
                    lineStr = "????????";
                }
                if (valueChanged)
                {
                    g.DrawString(lineStr, this.Font, textChangedBrush, col2ValueX, lineY);
                }
                else
                {
                    g.DrawString(lineStr, this.Font, textBrush, col2ValueX, lineY);
                }


                lineY += GetLineHeight();
            }

            lineY += GetLineHeight();


            g.DrawString("LR", this.Font, textBrush, col1NameX, lineY);

            valueChanged = false;
            if (info != null)
            {
                lineStr = String.Format("{0:X8}", info.lr);
                if (oldInfo != null)
                {
                    valueChanged = info.lr != oldInfo.lr;
                }
            }
            else
            {
                lineStr = "????????";
            }
            if (valueChanged)
            {
                g.DrawString(lineStr, this.Font, textChangedBrush, col1ValueX, lineY);
            }
            else
            {
                g.DrawString(lineStr, this.Font, textBrush, col1ValueX, lineY);
            }

            lineY += GetLineHeight();


            g.DrawString("CTR", this.Font, textBrush, col1NameX, lineY);

            valueChanged = false;
            if (info != null)
            {
                lineStr = String.Format("{0:X8}", info.ctr);
                if (oldInfo != null)
                {
                    valueChanged = info.ctr != oldInfo.ctr;
                }
            }
            else
            {
                lineStr = "????????";
            }
            if (valueChanged)
            {
                g.DrawString(lineStr, this.Font, textChangedBrush, col1ValueX, lineY);
            }
            else
            {
                g.DrawString(lineStr, this.Font, textBrush, col1ValueX, lineY);
            }

            lineY += GetLineHeight();


            // CRFs
            lineY += GetLineHeight();

            g.DrawString("- + Z O", this.Font, textBrush, col1ValueX, lineY);
            g.DrawString("- + Z O", this.Font, textBrush, col2ValueX, lineY);
            lineY += GetLineHeight();

            for (int i = 0; i < 4; ++i)
            {
                lineStr = String.Format("CRF{0}", i);
                g.DrawString(lineStr, this.Font, textBrush, col1NameX, lineY);
                g.DrawString(getCRFStr(i), this.Font, textBrush, col1ValueX, lineY);

                lineStr = String.Format("CRF{0}", 4 + i);
                g.DrawString(lineStr, this.Font, textBrush, col2NameX, lineY);
                g.DrawString(getCRFStr(4 + i), this.Font, textBrush, col2ValueX, lineY);

                lineY += GetLineHeight();
            }

        }

        private string getCRFStr(int crfNum)
        {
            if (info != null)
            {
                uint bits = info.crf >> crfNum;
                return String.Format("{0} {1} {2} {3}",
                    (bits >> 0) & 1,
                    (bits >> 1) & 1,
                    (bits >> 2) & 1,
                    (bits >> 3) & 1);
            } else
            {
                return "? ? ? ?";
            }
            
        }
    }
}
