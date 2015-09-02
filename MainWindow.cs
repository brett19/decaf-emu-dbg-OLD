using System;
using System.Windows.Forms;

namespace debugger
{
    public partial class MainWindow : Form
    {
        public AssemblyView asmView = null;
        public RegisterView regView = null;

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
            NetHandler.StartListening();

            asmView = new AssemblyView();
            asmView.MdiParent = this;
            asmView.Dock = DockStyle.Left;

            regView = new RegisterView();
            regView.MdiParent = this;
            regView.Dock = DockStyle.Right;
        }

        private void NetHandler_ConnectedEvent(object sender, ConnectedEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                statusLabel.Text = "Connected.";

                asmView.Show();
                regView.Show();
            });
        }

        private void NetHandler_DisconnectedEvent(object sender, DisconnectedEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate {
                statusLabel.Text = "Disconnected.";

                asmView.Hide();
                regView.Hide();
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

        private void NetHandler_BpHitEvent(object sender, BpHitEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate {
                statusLabel.Text = "Connected.  BpHit.";

                // For now, just immediately resume...
                NetHandler.SendResume();
            });
        }
    }
}
