using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace debugger
{
    public class WheelVScrollBar : VScrollBar
    {
        public void SendMouseWheelEvent(MouseEventArgs e)
        {
            this.OnMouseWheel(e);
        }
    }
}
