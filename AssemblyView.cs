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

            assemblyDisp.DebugManager = new EmuDebugManager();
        }

        public void UpdateData(DebugPauseInfo pauseInfo, DebugThreadInfo activeThread)
        {
            assemblyDisp.DebugManager.UpdateData(pauseInfo, activeThread);

            if (pauseInfo != null)
            {
                assemblyDisp.DataView = assemblyDisp.DebugManager.CreateMemoryView(0x00000000, 0x100000000);
                assemblyDisp.ActiveAddress = activeThread.cia;
                assemblyDisp.JumpToAddress(activeThread.cia);
            } else
            {
                assemblyDisp.DataView = null;
            }
        }

    }
}
