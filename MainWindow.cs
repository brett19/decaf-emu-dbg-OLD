﻿using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace debugger
{
    public partial class MainWindow : Form
    {
        public AssemblyView asmView = null;
        public RegisterView regView = null;
        public ModulesView modView = null;
        public ThreadsView thrdView = null;
        public StackView stackView = null;
        public MemoryView memView = null;

        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void MainWindow_Load(object sender, EventArgs e)
        {
            NetHandler.ConnectedEvent += NetHandler_ConnectedEvent;
            NetHandler.DisconnectedEvent += NetHandler_DisconnectedEvent;
            NetHandler.BpHitEvent += NetHandler_BpHitEvent;
            NetHandler.CoreSteppedEvent += NetHandler_CoreSteppedEvent;
            NetHandler.PausedEvent += NetHandler_PausedEvent;
            NetHandler.GetTraceEvent += NetHandler_GetTraceEvent;
            NetHandler.StartListening();

            asmView = new AssemblyView();
            asmView.MdiParent = this;
            asmView.FormClosing += MainWindow_FormClosing;
            
            regView = new RegisterView();
            regView.MdiParent = this;
            regView.FormClosing += MainWindow_FormClosing;

            modView = new ModulesView();
            modView.MdiParent = this;
            modView.FormClosing += MainWindow_FormClosing;

            thrdView = new ThreadsView();
            thrdView.MdiParent = this;
            thrdView.FormClosing += MainWindow_FormClosing;

            stackView = new StackView();
            stackView.MdiParent = this;
            stackView.FormClosing += MainWindow_FormClosing;

            memView = new MemoryView();
            memView.MdiParent = this;
            memView.FormClosing += MainWindow_FormClosing;

            assemblyToolStripMenuItem.Checked = true;
            registersToolStripMenuItem.Checked = true;
            stackToolStripMenuItem.Checked = true;
            memoryToolStripMenuItem.Checked = true;

            int parentWidth = this.ClientSize.Width - 4;
            int parentHeight = this.ClientSize.Height - 60;

            regView.Left = parentWidth - regView.Size.Width;
            regView.Top = 0;
            regView.Height = parentHeight / 3 * 2;

            asmView.Left = 0;
            asmView.Top = 0;
            asmView.Width = parentWidth - regView.Width;
            asmView.Height = parentHeight / 3 * 2;

            memView.Left = 0;
            memView.Top = asmView.Height;
            memView.Width = asmView.Width;
            memView.Height = parentHeight - asmView.Height;

            stackView.Left = regView.Left;
            stackView.Top = memView.Top;
            stackView.Width = regView.Width;
            stackView.Height = memView.Height;
        }

        private DebugTraceEntry[] TraceEntries = null;
        private int TraceEntryIdx = 0;

        private void NetHandler_GetTraceEvent(object sender, GetTraceEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate {
                stepBackToolStripMenuItem.Enabled = true;
                stepForwardToolStripMenuItem.Enabled = true;

                TraceEntries = e.Info;
                TraceEntryIdx = 0;

                Tracing = true;
                TraceActiveThread = ActiveThread.Clone();
                applyTraceFields(TraceEntries[TraceEntryIdx]);
            });
        }

        private void stepTraceData(int traceIdx)
        {
            if (traceIdx == TraceEntryIdx)
            {
                // Already here, do nothing
                return;
            } else if (traceIdx > TraceEntryIdx)
            {
                // Stepping Backwards, we copy the TraceThread for RegView update tracking
                TraceActiveThread = TraceActiveThread.Clone();
                for (var i = TraceEntryIdx + 1; i <= traceIdx; ++i)
                {
                    applyTraceFields(TraceEntries[i]);
                }
                TraceEntryIdx = traceIdx;
            }
            else if (traceIdx < TraceEntryIdx)
            {
                // Stepping 'forward', need to rescan
                TraceActiveThread = ActiveThread.Clone();
                for (var i = 0; i <= traceIdx; ++i)
                {
                    applyTraceFields(TraceEntries[i]);
                }
                TraceEntryIdx = traceIdx;
            }

            UpdatePauseInfo();
        }

        private void applyTraceFields(DebugTraceEntry trace)
        {
            foreach(DebugTraceEntryField field in trace.fields)
            {
                TraceActiveThread.ApplyTraceData(field.type, field.data);
            }
            TraceActiveThread.cia = trace.cia;
        }

        private void stepBackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (TraceEntryIdx < TraceEntries.Length - 1)
            {
                stepTraceData(TraceEntryIdx + 1);
            } else
            {
                statusLabel.Text = "No more backtrace entries.";
            }
        }

        private void stepForwardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (TraceEntryIdx > 0)
            {
                stepTraceData(TraceEntryIdx - 1);
            } else
            {
                statusLabel.Text = "No more backtrace entries.";
            }
        }

        private void NetHandler_ConnectedEvent(object sender, ConnectedEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                statusLabel.Text = "Connected.";
            });
        }

        private void NetHandler_DisconnectedEvent(object sender, DisconnectedEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate {
                statusLabel.Text = "Disconnected.";

                UpdatePauseInfo(null);
            });
        }

        private static DebugThreadInfo getActiveThread(DebugThreadInfo[] threads, uint coreId)
        {
            for (var i = 0; i < threads.Length; ++i)
            {
                if (threads[i].curCoreId == coreId)
                {
                    return threads[i];
                }
            }
            return null;
        }

        private DebugPauseInfo PauseInfo = null;
        private DebugThreadInfo ActiveThread = null;

        private bool Tracing = false;
        private DebugThreadInfo TraceActiveThread = null;

        private void UpdatePauseInfo()
        {
            if (Tracing)
            {
                modView.Enabled = false;
                thrdView.Enabled = false;
                asmView.UpdateData(PauseInfo, TraceActiveThread);
                regView.UpdateData(PauseInfo, TraceActiveThread);
                memView.Enabled = false;
                stackView.Enabled = false;
            } else
            {
                modView.UpdateData(PauseInfo, ActiveThread);
                thrdView.UpdateData(PauseInfo, ActiveThread);
                asmView.UpdateData(PauseInfo, ActiveThread);
                regView.UpdateData(PauseInfo, ActiveThread);
                memView.UpdateData(PauseInfo, ActiveThread);
                stackView.UpdateData(PauseInfo, ActiveThread);
            }
        }

        private void UpdatePauseInfo(DebugPauseInfo pauseInfo, uint activeCore = 1)
        {
            PauseInfo = pauseInfo;
            if (pauseInfo != null)
            {
                ActiveThread = getActiveThread(pauseInfo.threads, activeCore);
            } else
            {
                ActiveThread = null;
            }
            
            UpdatePauseInfo();
            asmView.Focus();
        }

        public void SetActiveThreadIdx(int threadIdx)
        {
            ActiveThread = PauseInfo.threads[threadIdx];
            UpdatePauseInfo();
        }

        private void NetHandler_BpHitEvent(object sender, BpHitEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate {
                statusLabel.Text = "Connected.  BpHit.";
                
                UpdatePauseInfo(e.PauseInfo, e.coreId);

                // TODO: Remove Single Step breakpoints...
            });
        }

        private void NetHandler_CoreSteppedEvent(object sender, CoreSteppedEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate {
                statusLabel.Text = "Connected.  Core Stepped.";

                UpdatePauseInfo(e.PauseInfo, e.coreId);
            });
        }

        private void NetHandler_PausedEvent(object sender, PausedEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate {
                statusLabel.Text = "Connected.  Paused.";

                UpdatePauseInfo(e.PauseInfo);
            });
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.MdiFormClosing)
            {
                return;
            }

            if (sender == asmView)
            {
                assemblyToolStripMenuItem.Checked = false;
                e.Cancel = true;
            } else if (sender == regView)
            {
                registersToolStripMenuItem.Checked = false;
                e.Cancel = true;
            } else if (sender == modView)
            {
                modulesToolStripMenuItem.Checked = false;
                e.Cancel = true;
            } else if (sender == thrdView)
            {
                threadsToolStripMenuItem.Checked = false;
                e.Cancel = true;
            }
        }

        private void continueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NetHandler.SendResume();
        }

        private void stepIntoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveThread != null && ActiveThread.curCoreId >= 0)
            {
                NetHandler.SendStepCore((uint)ActiveThread.curCoreId);
            }
            else
            {
                MessageBox.Show("Cannot step current thread as it is not active on any core.");
            }
        }

        private void stepOverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveThread != null && ActiveThread.curCoreId >= 0)
            {
                NetHandler.SendStepCoreOver((uint)ActiveThread.curCoreId);
            }
            else
            {
                MessageBox.Show("Cannot step current thread as it is not active on any core.");
            }
        }

        private void assemblyToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (assemblyToolStripMenuItem.Checked)
            {
                asmView.Show();
            }
            else
            {
                asmView.Hide();
            }
        }

        private void registersToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (assemblyToolStripMenuItem.Checked)
            {
                regView.Show();
            }
            else
            {
                regView.Hide();
            }
        }
        
        private void modulesToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (modulesToolStripMenuItem.Checked)
            {
                modView.Show();
            }
            else
            {
                modView.Hide();
            }
        }

        private void threadsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (threadsToolStripMenuItem.Checked)
            {
                thrdView.Show();
            }
            else
            {
                thrdView.Hide();
            }
        }

        private void memoryToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (memoryToolStripMenuItem.Checked)
            {
                memView.Show();
            }
            else
            {
                memView.Hide();
            }
        }

        private void stackToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (stackToolStripMenuItem.Checked)
            {
                stackView.Show();
            }
            else
            {
                stackView.Hide();
            }
        }

        private void breakAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NetHandler.SendPause();
        }

        private void backtraceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveThread != null)
            {
                NetHandler.SendGetTrace(ActiveThread.id);
            }
        }

    }
}
