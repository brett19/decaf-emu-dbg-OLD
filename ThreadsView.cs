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
    public partial class ThreadsView : Form
    {
        public ThreadsView()
        {
            InitializeComponent();
        }

        private void ThreadsView_Load(object sender, EventArgs e)
        {

        }

        public void UpdateData(DebugThreadInfo[] info, DebugThreadInfo activeThread)
        {
            listView.Items.Clear();
            for (int i = 0; i < info.Length; ++i)
            {
                ListViewItem item = new ListViewItem();

                if (info[i] == activeThread)
                {
                    item.Text = ">";
                } else
                {
                    item.Text = "";
                }

                item.SubItems.Add(i.ToString());
                item.SubItems.Add(info[i].name);
                if (info[i].curCoreId >= 0)
                {
                    item.SubItems.Add(info[i].curCoreId.ToString());
                } else
                {
                    item.SubItems.Add("");
                }
                item.SubItems.Add(String.Format("{0:X8}", info[i].cia));
                listView.Items.Add(item);
            }
        }

        private void listView_DoubleClick(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0) {
                var selItem = listView.SelectedItems[0];
                ((MainWindow)this.MdiParent).SetActiveThreadIdx(selItem.Index);
            }
        }
    }
}
