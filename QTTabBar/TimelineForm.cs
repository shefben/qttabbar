using System;
using System.Linq;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal class TimelineForm : Form {
        private TextBox txtFilter = new TextBox();
        private ListBox lst = new ListBox();
        private Button btnClose = new Button();

        public TimelineForm() {
            Text = "Navigation Timeline";
            Width = 600; Height = 400; StartPosition = FormStartPosition.CenterParent;
            txtFilter.Dock = DockStyle.Top; // no placeholder on .NET 3.5
            lst.Dock = DockStyle.Fill;
            btnClose.Text = "Close"; btnClose.Dock = DockStyle.Bottom; btnClose.Height = 28; btnClose.Click += (s,e)=>Close();
            Controls.Add(lst); Controls.Add(btnClose); Controls.Add(txtFilter);
            txtFilter.TextChanged += (s,e)=> RefreshList();
            lst.DoubleClick += (s,e)=> NavigateSelected();
            Load += (s,e)=> { RefreshList(); txtFilter.Focus(); };
        }

        private void RefreshList() {
            var data = TimelineManager.Load(1000);
            var q = data.Select(d => new { d.Utc, d.Path });
            string f = (txtFilter.Text == null) ? string.Empty : txtFilter.Text.Trim();
            if (!string.IsNullOrEmpty(f)) q = q.Where(x => x.Path.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
            lst.Items.Clear();
            foreach (var x in q) lst.Items.Add(string.Format("{0:yyyy-MM-dd HH:mm:ss}  {1}", x.Utc.ToLocalTime(), x.Path));
        }

        private void NavigateSelected() {
            if (lst.SelectedItem == null) return;
            string line = lst.SelectedItem.ToString();
            int i = line.IndexOf("  "); if (i <= 0) return; string path = line.Substring(i+2);
            var bar = InstanceManager.GetThreadTabBar();
            if (bar == null) return;
            using (var idl = new IDLWrapper(path)) {
                if (idl.Available) bar.OpenNewTab(idl, false, true);
            }
            Close();
        }
    }
}
