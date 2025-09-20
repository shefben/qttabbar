using System;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal class OperationHistoryForm : Form {
        private ListBox lst = new ListBox();
        private Button btnClose = new Button();
        public OperationHistoryForm() {
            Text = "Operation History"; Width=800; Height=500; StartPosition = FormStartPosition.CenterParent;
            lst.Dock = DockStyle.Fill; btnClose.Text = "Close"; btnClose.Dock = DockStyle.Bottom; btnClose.Height=28; btnClose.Click += (s,e)=>Close();
            Controls.Add(lst); Controls.Add(btnClose);
            Load += (s,e)=> { try { foreach (var line in OperationLedgerManager.LoadRecent()) lst.Items.Add(line); } catch { } };
        }
    }
}

