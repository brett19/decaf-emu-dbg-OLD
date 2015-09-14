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
    public partial class MemoryView : Form
    {
        public MemoryView()
        {
            InitializeComponent();

            memDisp.DebugManager = new EmuDebugManager();
        }

        public void UpdateData(DebugPauseInfo pauseInfo, DebugThreadInfo activeThread)
        {
            memDisp.DebugManager.UpdateData(pauseInfo, activeThread);

            if (activeThread != null)
            {
                uint stackCurrent = activeThread.gpr[1];
                memDisp.DataView = memDisp.DebugManager.CreateMemoryView(0x00000000, 0x100000000);
                memDisp.ActiveAddress = stackCurrent;
                memDisp.JumpToAddress(stackCurrent);
            }
            else
            {
                memDisp.DataView = null;
            }
        }
    }
}
