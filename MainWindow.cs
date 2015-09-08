using System;
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

        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void MainWindow_Load(object sender, EventArgs e)
        {
            NetHandler.ConnectedEvent += NetHandler_ConnectedEvent;
            NetHandler.DisconnectedEvent += NetHandler_DisconnectedEvent;
            NetHandler.PrelaunchEvent += NetHandler_PrelaunchEvent;
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

            assemblyToolStripMenuItem.Checked = true;
            registersToolStripMenuItem.Checked = true;
            threadsToolStripMenuItem.Checked = true;
            stackToolStripMenuItem.Checked = true;

            int parentWidth = this.ClientSize.Width - 4;
            int parentHeight = this.ClientSize.Height - 60;

            regView.Left = parentWidth - regView.Size.Width;
            regView.Top = 0;
            regView.Height = parentHeight / 3 * 2;

            asmView.Left = 0;
            asmView.Top = 0;
            asmView.Width = parentWidth - regView.Width;
            asmView.Height = parentHeight / 3 * 2;

            thrdView.Left = 0;
            thrdView.Top = asmView.Height;
            thrdView.Width = asmView.Width;
            thrdView.Height = parentHeight - asmView.Height;

            stackView.Left = regView.Left;
            stackView.Top = thrdView.Top;
            stackView.Width = regView.Width;
            stackView.Height = thrdView.Height;
        }

        private void NetHandler_GetTraceEvent(object sender, GetTraceEventArgs e)
        {
            throw new NotImplementedException();
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

        private void NetHandler_PrelaunchEvent(object sender, PrelaunchEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                statusLabel.Text = "Connected.  Prelaunch.";

                var pauseInfo = e.PauseInfo;
                var userModule = pauseInfo.modules[pauseInfo.userModuleIdx];

                // Add breakpoint at Entrypoint
                NetHandler.SendAddBreakpoint(userModule.entryPoint, 0);
                NetHandler.SendResume();
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
            modView.UpdateData(PauseInfo, ActiveThread);
            thrdView.UpdateData(PauseInfo, ActiveThread);
            asmView.UpdateData(PauseInfo, ActiveThread);
            regView.UpdateData(PauseInfo, ActiveThread);
            stackView.UpdateData(PauseInfo, ActiveThread);
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
            if (ActiveThread.curCoreId >= 0)
            {
                NetHandler.SendStepCore((uint)ActiveThread.curCoreId);
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
