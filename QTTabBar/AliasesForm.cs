using System;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal class AliasesForm : Form {
        private TextBox txtAlias = new TextBox();
        private TextBox txtTarget = new TextBox();
        private Button btnSave = new Button();
        private Button btnClose = new Button();
        public AliasesForm(string currentPath) {
            Text = "Path Aliases"; Width=520; Height=160; StartPosition=FormStartPosition.CenterParent;
            var lbl1 = new Label{Text="Alias (e.g., @proj)", Left=12, Top=12, Width=140};
            var lbl2 = new Label{Text="Target Path", Left=12, Top=44, Width=140};
            txtAlias.Left=160; txtAlias.Top=10; txtAlias.Width=320; txtAlias.Text="@";
            txtTarget.Left=160; txtTarget.Top=42; txtTarget.Width=320; txtTarget.Text = currentPath ?? string.Empty;
            btnSave.Text="Save"; btnSave.Left=320; btnSave.Top=80; btnSave.Click += (s,e)=> { try { AliasManager.Set(txtAlias.Text.Trim(), txtTarget.Text.Trim()); Close(); } catch { } };
            btnClose.Text="Close"; btnClose.Left=400; btnClose.Top=80; btnClose.Click += (s,e)=> Close();
            Controls.Add(lbl1); Controls.Add(lbl2); Controls.Add(txtAlias); Controls.Add(txtTarget); Controls.Add(btnSave); Controls.Add(btnClose);
        }
    }
}

