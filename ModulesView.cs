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
    public partial class ModulesView : Form
    {
        public ModulesView()
        {
            InitializeComponent();
        }

        private void ModulesView_Load(object sender, EventArgs e)
        {

        }

        public void UpdateData(DebugPauseInfo pauseInfo, DebugThreadInfo activeThread)
        {
            listBox.Items.Clear();
            if (pauseInfo != null)
            {
                var info = pauseInfo.modules;
                for (int i = 0; i < info.Length; ++i)
                {
                    string lineText = String.Format("{0}  EP@{1:X8}",
                        info[i].name,
                        info[i].entryPoint);
                    listBox.Items.Add(lineText);
                }
            }
        }
    }
}
