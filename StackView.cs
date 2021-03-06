﻿using System;
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

            stackDisp.DebugManager = new EmuDebugManager();
        }
        
        public void UpdateData(DebugPauseInfo pauseInfo, DebugThreadInfo activeThread)
        {
            stackDisp.DebugManager.UpdateData(pauseInfo, activeThread);

            if (activeThread != null)
            {
                uint stackCurrent = activeThread.gpr[1];
                stackDisp.DataView = stackDisp.DebugManager.CreateMemoryView(activeThread.stackEnd - 4, activeThread.stackStart);
                stackDisp.ActiveAddress = stackCurrent;
                stackDisp.JumpToAddress(stackCurrent);

            } else
            {
                stackDisp.DataView = null;
            }
        }
    }
}
