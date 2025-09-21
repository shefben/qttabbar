using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal sealed class TagColorEditorForm : Form {
        private readonly ListView lstTags;
        private readonly Button btnSetColor;
        private readonly Button btnClearColor;
        private readonly Button btnOk;
        private readonly Button btnCancel;
        private readonly ToolTip paletteToolTip;

        private readonly Dictionary<string, Color?> initialColors;
        private readonly Dictionary<string, Color?> workingColors;

        private static Color? lastChosenColor;

        private TagColorEditorForm(IList<string> tags, IDictionary<string, Color?> existingColors) {
            Text = "Tag Colors";
            ClientSize = new Size(440, 380);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            initialColors = new Dictionary<string, Color?>(StringComparer.OrdinalIgnoreCase);
            workingColors = new Dictionary<string, Color?>(StringComparer.OrdinalIgnoreCase);
            paletteToolTip = new ToolTip();

            IEnumerable<string> tagSequence = tags ?? new string[0];
            foreach(string tag in tagSequence) {
                Color? color = null;
                if(existingColors != null && existingColors.TryGetValue(tag, out color)) {
                    // Preserve existing value
                }
                initialColors[tag] = color;
                workingColors[tag] = color;
            }

            int margin = 12;
            int contentWidth = ClientSize.Width - (margin * 2);

            lstTags = new ListView {
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = true,
                Left = margin,
                Top = margin,
                Width = contentWidth,
                Height = 190,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            int colorColumnWidth = Math.Max(120, lstTags.Width / 3);
            lstTags.Columns.Add("Tag", lstTags.Width - colorColumnWidth - 4, HorizontalAlignment.Left);
            lstTags.Columns.Add("Color", colorColumnWidth, HorizontalAlignment.Left);
            PopulateList();

            Label lblPalette = new Label {
                Text = "Quick palette:",
                AutoSize = true,
                Left = margin,
                Top = lstTags.Bottom + 8
            };

            FlowLayoutPanel defaultPalettePanel = CreatePalettePanel(TagColorPalette.GetDefaultPalette(), "Palette color");
            defaultPalettePanel.Left = margin;
            defaultPalettePanel.Top = lblPalette.Bottom + 4;
            defaultPalettePanel.Width = contentWidth;

            Label lblHighContrast = new Label {
                Text = "High contrast palette:",
                AutoSize = true,
                Left = margin,
                Top = defaultPalettePanel.Bottom + 6
            };

            FlowLayoutPanel highContrastPanel = CreatePalettePanel(TagColorPalette.GetHighContrastPalette(), "High contrast color");
            highContrastPanel.Left = margin;
            highContrastPanel.Top = lblHighContrast.Bottom + 4;
            highContrastPanel.Width = contentWidth;

            int buttonRowTop = highContrastPanel.Bottom + 12;

            btnSetColor = new Button {
                Text = "Choose Color...",
                Left = margin,
                Top = buttonRowTop,
                Width = 124
            };
            btnSetColor.Click += (sender, args) => ChooseColorForSelection();

            btnClearColor = new Button {
                Text = "Clear",
                Left = btnSetColor.Right + 8,
                Top = btnSetColor.Top,
                Width = 90
            };
            btnClearColor.Click += (sender, args) => ClearColorForSelection();

            btnOk = new Button {
                Text = "OK",
                Width = 90,
                DialogResult = DialogResult.OK
            };
            btnCancel = new Button {
                Text = "Cancel",
                Width = 90,
                DialogResult = DialogResult.Cancel
            };

            btnCancel.Left = ClientSize.Width - margin - btnCancel.Width;
            btnCancel.Top = buttonRowTop;
            btnOk.Left = btnCancel.Left - 10 - btnOk.Width;
            btnOk.Top = buttonRowTop;

            Controls.Add(lstTags);
            Controls.Add(lblPalette);
            Controls.Add(defaultPalettePanel);
            Controls.Add(lblHighContrast);
            Controls.Add(highContrastPanel);
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
                else {
                    Color? suggestion = TagColorPalette.SuggestColor(workingColors.Values);
                    if(suggestion.HasValue) {
                        dialog.Color = suggestion.Value;
                    }
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

        private FlowLayoutPanel CreatePalettePanel(IEnumerable<Color> palette, string accessiblePrefix) {
            FlowLayoutPanel panel = new FlowLayoutPanel {
                Height = 36,
                AutoSize = false,
                WrapContents = false,
                AutoScroll = true,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            foreach(Color color in palette ?? Enumerable.Empty<Color>()) {
                Button button = CreatePaletteButton(color, accessiblePrefix);
                panel.Controls.Add(button);
            }
            return panel;
        }

        private Button CreatePaletteButton(Color color, string accessiblePrefix) {
            Button button = new Button {
                Width = 28,
                Height = 28,
                Margin = new Padding(2),
                BackColor = color,
                FlatStyle = FlatStyle.Flat,
                Tag = color,
                UseVisualStyleBackColor = false,
                Text = string.Empty
            };
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = ControlPaint.Dark(color);
            button.AccessibleName = accessiblePrefix + " " + ColorToDisplay(color);
            paletteToolTip.SetToolTip(button, ColorToDisplay(color));
            button.Click += PaletteButton_Click;
            return button;
        }

        private void PaletteButton_Click(object sender, EventArgs e) {
            Button button = sender as Button;
            if(button == null) {
                return;
            }
            Color color = (Color)button.Tag;
            ApplyPaletteColor(color);
        }

        private void ApplyPaletteColor(Color color) {
            if(lstTags.SelectedItems.Count == 0) {
                try {
                    SystemSounds.Beep.Play();
                }
                catch { }
                lastChosenColor = color;
                return;
            }
            lastChosenColor = color;
            foreach(ListViewItem item in lstTags.SelectedItems) {
                string tag = item.Tag as string;
                if(string.IsNullOrEmpty(tag)) {
                    continue;
                }
                workingColors[tag] = color;
                item.SubItems[1].Text = ColorToDisplay(color);
            }
        }

        protected override void Dispose(bool disposing) {
            if(disposing) {
                if(paletteToolTip != null) {
                    paletteToolTip.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}

