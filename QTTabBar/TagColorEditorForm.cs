using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal sealed class TagColorEditorForm : Form {
        private readonly ListView lstTags;
        private readonly Button btnSetColor;
        private readonly Button btnClearColor;
        private readonly Button btnOk;
        private readonly Button btnCancel;

        private readonly Dictionary<string, Color?> initialColors;
        private readonly Dictionary<string, Color?> workingColors;

        private static Color? lastChosenColor;

        private TagColorEditorForm(IList<string> tags, IDictionary<string, Color?> existingColors) {
            Text = "Tag Colors";
            ClientSize = new Size(400, 260);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            initialColors = new Dictionary<string, Color?>(StringComparer.OrdinalIgnoreCase);
            workingColors = new Dictionary<string, Color?>(StringComparer.OrdinalIgnoreCase);
            IEnumerable<string> tagSequence = tags ?? new string[0];
            foreach(string tag in tagSequence) {
                Color? color = null;
                if(existingColors != null && existingColors.TryGetValue(tag, out color)) {
                    // nothing else to do
                }
                initialColors[tag] = color;
                workingColors[tag] = color;
            }

            lstTags = new ListView {
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = true,
                Left = 12,
                Top = 12,
                Width = 380,
                Height = 200,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lstTags.Columns.Add("Tag", 220, HorizontalAlignment.Left);
            lstTags.Columns.Add("Color", 140, HorizontalAlignment.Left);
            PopulateList();

            btnSetColor = new Button {
                Text = "Choose Color...",
                Left = 12,
                Top = lstTags.Bottom + 8,
                Width = 120
            };
            btnSetColor.Click += (sender, args) => ChooseColorForSelection();

            btnClearColor = new Button {
                Text = "Clear",
                Left = btnSetColor.Right + 8,
                Top = btnSetColor.Top,
                Width = 80
            };
            btnClearColor.Click += (sender, args) => ClearColorForSelection();

            btnOk = new Button {
                Text = "OK",
                Width = 80,
                DialogResult = DialogResult.OK
            };
            btnCancel = new Button {
                Text = "Cancel",
                Width = 80,
                DialogResult = DialogResult.Cancel
            };

            btnOk.Left = ClientSize.Width - 190;
            btnOk.Top = ClientSize.Height - 50;
            btnCancel.Left = btnOk.Right + 10;
            btnCancel.Top = btnOk.Top;

            Controls.Add(lstTags);
            Controls.Add(btnSetColor);
            Controls.Add(btnClearColor);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        internal static bool TryEditColors(IWin32Window owner, IList<string> tags, IDictionary<string, Color?> existingColors, out Dictionary<string, Color?> result) {
            using(TagColorEditorForm form = new TagColorEditorForm(tags, existingColors)) {
                DialogResult dialogResult = form.ShowDialog(owner);
                if(dialogResult == DialogResult.OK) {
                    result = form.BuildResult();
                    return true;
                }
            }
            result = null;
            return false;
        }

        private void PopulateList() {
            lstTags.BeginUpdate();
            lstTags.Items.Clear();
            foreach(KeyValuePair<string, Color?> pair in workingColors.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)) {
                ListViewItem item = new ListViewItem(pair.Key) { Tag = pair.Key };
                item.SubItems.Add(ColorToDisplay(pair.Value));
                lstTags.Items.Add(item);
            }
            lstTags.EndUpdate();
        }

        private void ChooseColorForSelection() {
            if(lstTags.SelectedItems.Count == 0) {
                return;
            }
            using(ColorDialog dialog = new ColorDialog { FullOpen = true }) {
                if(lastChosenColor.HasValue) {
                    dialog.Color = lastChosenColor.Value;
                }
                if(dialog.ShowDialog(this) != DialogResult.OK) {
                    return;
                }
                lastChosenColor = dialog.Color;
                foreach(ListViewItem item in lstTags.SelectedItems) {
                    string tag = item.Tag as string;
                    if(tag == null) {
                        continue;
                    }
                    workingColors[tag] = dialog.Color;
                    item.SubItems[1].Text = ColorToDisplay(dialog.Color);
                }
            }
        }

        private void ClearColorForSelection() {
            if(lstTags.SelectedItems.Count == 0) {
                return;
            }
            foreach(ListViewItem item in lstTags.SelectedItems) {
                string tag = item.Tag as string;
                if(tag == null) {
                    continue;
                }
                workingColors[tag] = null;
                item.SubItems[1].Text = ColorToDisplay(null);
            }
        }

        private Dictionary<string, Color?> BuildResult() {
            Dictionary<string, Color?> changes = new Dictionary<string, Color?>(StringComparer.OrdinalIgnoreCase);
            foreach(KeyValuePair<string, Color?> pair in workingColors) {
                Color? initial = null;
                initialColors.TryGetValue(pair.Key, out initial);
                if(!ColorsEqual(initial, pair.Value)) {
                    changes[pair.Key] = pair.Value;
                }
            }
            return changes;
        }

        private static bool ColorsEqual(Color? first, Color? second) {
            if(!first.HasValue && !second.HasValue) {
                return true;
            }
            if(first.HasValue != second.HasValue) {
                return false;
            }
            return first.Value.ToArgb() == second.Value.ToArgb();
        }

        private static string ColorToDisplay(Color? color) {
            if(!color.HasValue) {
                return "None";
            }
            Color c = color.Value;
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
    }
}
