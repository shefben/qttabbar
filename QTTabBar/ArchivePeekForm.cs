using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal class ArchivePeekForm : Form {
        private ListBox lst = new ListBox();
        private Button btnExtract = new Button();
        private Button btnClose = new Button();
        private string archivePath;
        private List<string> selected = new List<string>();

        public ArchivePeekForm(string path) {
            archivePath = path;
            Text = "Archive Peek - " + Path.GetFileName(path);
            Width=600; Height=450; StartPosition=FormStartPosition.CenterParent;
            lst.Dock = DockStyle.Fill; lst.SelectionMode = SelectionMode.MultiExtended;
            btnExtract.Text = "Extract Selected..."; btnExtract.Dock = DockStyle.Bottom; btnExtract.Height=28; btnExtract.Click += (s,e)=> ExtractSelected();
            btnClose.Text = "Close"; btnClose.Dock = DockStyle.Bottom; btnClose.Height=28; btnClose.Click += (s,e)=> Close();
            Controls.Add(lst); Controls.Add(btnExtract); Controls.Add(btnClose);
            Load += (s,e)=> LoadEntries();
        }

        private void LoadEntries() {
            try {
                lst.Items.Clear();
                string ext = Path.GetExtension(archivePath).ToLowerInvariant();
                if (SevenZipCli.Supports(archivePath) && (ext == ".7z" || ext == ".rar")) {
                    foreach (var n in SevenZipCli.ListEntries(archivePath)) lst.Items.Add(n);
                    if (lst.Items.Count == 0) lst.Items.Add("(No entries)");
                } else if (ShellZip.IsSupported(archivePath)) {
                    foreach (var n in ShellZip.ListEntries(archivePath)) lst.Items.Add(n);
                    if (lst.Items.Count == 0) lst.Items.Add("(No entries)");
                } else {
                    lst.Items.Add("(No preview provider for this archive)");
                }
            } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Archive Peek"); }
        }

        private void ExtractSelected() {
            try {
                if (lst.SelectedItems.Count == 0) return;
                using (var fbd = new FolderBrowserDialog()) {
                    if (fbd.ShowDialog(this) != DialogResult.OK) return;
                    var names = new List<string>(); foreach (var it in lst.SelectedItems) names.Add(Convert.ToString(it));
                    string ext = Path.GetExtension(archivePath).ToLowerInvariant();
                    bool ok = false;
                    if (SevenZipCli.Supports(archivePath) && (ext == ".7z" || ext == ".rar")) {
                        ok = SevenZipCli.Extract(archivePath, fbd.SelectedPath, names);
                    } else if (ShellZip.IsSupported(archivePath)) {
                        ok = ShellZip.Extract(archivePath, fbd.SelectedPath, names);
                    }
                    if (!ok) MessageBox.Show(this, "Extraction failed or provider not available.", "Extract");
                }
            } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Extract"); }
        }
    }
}
