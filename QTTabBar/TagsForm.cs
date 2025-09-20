using System;
using System.Linq;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal class TagsForm : Form {
        private TextBox txtTags = new TextBox();
        private Button btnApply = new Button();
        private Button btnClose = new Button();
        private string[] targets;
        public TagsForm(string[] paths) {
            targets = paths ?? new string[0];
            Text = "Tags"; Width=480; Height=140; StartPosition=FormStartPosition.CenterParent;
            var lbl = new Label{Text="Tags (comma separated)", Left=12, Top=12, Width=180};
            txtTags.Left=200; txtTags.Top=10; txtTags.Width=260;
            btnApply.Text="Apply"; btnApply.Left=300; btnApply.Top=50; btnApply.Click += (s,e)=> { try { var tags = (txtTags.Text??"").Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray(); TagManager.AddTags(targets, tags); Close(); } catch { } };
            btnClose.Text="Close"; btnClose.Left=380; btnClose.Top=50; btnClose.Click += (s,e)=> Close();
            Controls.Add(lbl); Controls.Add(txtTags); Controls.Add(btnApply); Controls.Add(btnClose);
        }
    }
}

