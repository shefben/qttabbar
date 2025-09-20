using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal sealed class CompareSplitForm : Form {
        private readonly CompareSplitHost splitHost;
        private readonly ListView summaryList;
        private readonly Label lblLeft;
        private readonly Label lblRight;
        private readonly Button btnCopyToRight;
        private readonly Button btnCopyToLeft;

        public CompareSplitForm(string left, string right) {
            Text = "Folder Compare";
            StartPosition = FormStartPosition.CenterParent;
            Width = 1280;
            Height = 780;

            var header = new TableLayoutPanel {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 2,
                Height = 76,
                Padding = new Padding(8, 8, 8, 0)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var lblLeftTitle = new Label { Text = "Left", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(Font, FontStyle.Bold) };
            var lblRightTitle = new Label { Text = "Right", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(Font, FontStyle.Bold) };
            lblLeft = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            lblRight = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };

            btnCopyToLeft = new Button { Text = "<- Copy", Dock = DockStyle.Fill };
            btnCopyToRight = new Button { Text = "Copy ->", Dock = DockStyle.Fill };
            btnCopyToLeft.Click += (s, e) => SyncToLeft();
            btnCopyToRight.Click += (s, e) => SyncToRight();

            header.Controls.Add(lblLeftTitle, 0, 0);
            header.Controls.Add(lblRightTitle, 1, 0);
            header.Controls.Add(btnCopyToLeft, 2, 0);
            header.Controls.Add(btnCopyToRight, 3, 0);
            header.Controls.Add(lblLeft, 0, 1);
            header.Controls.Add(lblRight, 1, 1);

            splitHost = new CompareSplitHost();
            splitHost.ComparisonUpdated += SplitHost_ComparisonUpdated;
            splitHost.Initialize(left, right);

            summaryList = new ListView {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false
            };
            summaryList.Columns.Add("Type", 160);
            summaryList.Columns.Add("Relative Path", 420);
            summaryList.Columns.Add("Details", 420);

            var mainSplit = new SplitContainer {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 6,
                Panel1MinSize = 300,
                Panel2MinSize = 160
            };
            mainSplit.Panel1.Controls.Add(splitHost);
            mainSplit.Panel2.Controls.Add(summaryList);

            Controls.Add(mainSplit);
            Controls.Add(header);

            FormClosed += (s, e) => CompareOverlayManager.ClearDetailedContext();
        }

        private void SplitHost_ComparisonUpdated(object sender, ComparisonResultEventArgs e) {
            try {
                lblLeft.Text = splitHost.CurrentLeftPath;
                lblRight.Text = splitHost.CurrentRightPath;
                PopulateSummary(e.Result);
            } catch { }
        }

        private void PopulateSummary(ComparisonResult result) {
            summaryList.BeginUpdate();
            summaryList.Items.Clear();
            if (result != null) {
                foreach (ComparisonRow row in result.Rows) {
                if (row.Category == ComparisonDiffType.Identical) {
                    continue;
                }
                ListViewItem item = new ListViewItem(GetRowType(row));
                item.SubItems.Add(row.RelativePath);
                item.SubItems.Add(GetRowDescription(row));
                summaryList.Items.Add(item);
            }
            }
            summaryList.EndUpdate();
        }

        private static string GetRowType(ComparisonRow row) {
            if (row.IsDirectory) return "Folder";
            return "File";
        }

        private static string GetRowDescription(ComparisonRow row) {
            switch (row.Category) {
                case ComparisonDiffType.LeftOnly:
                    return "Only in left";
                case ComparisonDiffType.RightOnly:
                    return "Only in right";
                case ComparisonDiffType.FileMismatch:
                    return "Different contents";
                case ComparisonDiffType.FolderExtraLeft:
                    return "Folder has extra items on left";
                case ComparisonDiffType.FolderExtraRight:
                    return "Folder has extra items on right";
                case ComparisonDiffType.FolderExtraBoth:
                    return "Folder has unmatched items on both sides";
                case ComparisonDiffType.FolderContentMismatch:
                    return "Folder contains differing files";
                default:
                    return string.Empty;
            }
        }

        private void SyncToRight() {
            ComparisonResult result = splitHost.CurrentResult;
            if (result == null) return;
            try {
                PerformSync(result.RemovedRelativeDirectories, result.RemovedRelativeFiles, result.ChangedRelativeFiles, result.LeftRoot, result.RightRoot);
                MessageBox.Show(this, "Copied differences to right.", "Compare", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch (Exception ex) {
                MessageBox.Show(this, ex.Message, "Sync", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SyncToLeft() {
            ComparisonResult result = splitHost.CurrentResult;
            if (result == null) return;
            try {
                PerformSync(result.AddedRelativeDirectories, result.AddedRelativeFiles, result.ChangedRelativeFiles, result.RightRoot, result.LeftRoot);
                MessageBox.Show(this, "Copied differences to left.", "Compare", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch (Exception ex) {
                MessageBox.Show(this, ex.Message, "Sync", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void PerformSync(IEnumerable<string> directories, IEnumerable<string> files, IEnumerable<string> changedFiles, string sourceRoot, string targetRoot) {
            if (directories != null) {
                foreach (string relative in directories) {
                    string source = Path.Combine(sourceRoot, relative);
                    string target = Path.Combine(targetRoot, relative);
                    CopyDirectoryRecursive(source, target);
                }
            }
            if (files != null) {
                foreach (string relative in files) {
                    string source = Path.Combine(sourceRoot, relative);
                    string target = Path.Combine(targetRoot, relative);
                    CopyFile(source, target, false);
                }
            }
            if (changedFiles != null) {
                foreach (string relative in changedFiles) {
                    string source = Path.Combine(sourceRoot, relative);
                    string target = Path.Combine(targetRoot, relative);
                    CopyFile(source, target, true);
                }
            }
        }

        private static void CopyDirectoryRecursive(string source, string target) {
            if (!Directory.Exists(source)) return;
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source)) {
                string dest = Path.Combine(target, Path.GetFileName(file));
                CopyFile(file, dest, true);
            }
            foreach (string dir in Directory.GetDirectories(source)) {
                string dest = Path.Combine(target, Path.GetFileName(dir));
                CopyDirectoryRecursive(dir, dest);
            }
        }

        private static void CopyFile(string source, string target, bool overwrite) {
            if (!File.Exists(source)) return;
            string folder = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            File.Copy(source, target, overwrite);
        }
    }
}
