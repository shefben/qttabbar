using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal class TagsForm : Form {
        private readonly TextBox txtTags = new TextBox();
        private readonly Button btnApply = new Button();
        private readonly Button btnClose = new Button();
        private readonly string[] targets;

        public TagsForm(string[] paths) {
            targets = paths ?? new string[0];
            Text = "Tags";
            Width = 480;
            Height = 160;
            StartPosition = FormStartPosition.CenterParent;

            var lbl = new Label { Text = "Tags (comma separated)", Left = 12, Top = 12, Width = 180 };
            txtTags.Left = 200;
            txtTags.Top = 10;
            txtTags.Width = 260;
            if(targets.Length == 1) {
                try {
                    txtTags.Text = TagManager.GetTagSummary(targets[0]);
                }
                catch { }
            }

            btnApply.Text = "Apply";
            btnApply.Left = 300;
            btnApply.Top = 60;
            btnApply.Click += (s, e) => ApplyTags();

            btnClose.Text = "Close";
            btnClose.Left = 380;
            btnClose.Top = 60;
            btnClose.Click += (s, e) => Close();

            Controls.Add(lbl);
            Controls.Add(txtTags);
            Controls.Add(btnApply);
            Controls.Add(btnClose);

            AcceptButton = btnApply;
            CancelButton = btnClose;
        }

        private void ApplyTags() {
            List<string> parsedTags = ParseTags(txtTags.Text).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if(parsedTags.Count == 0) {
                Close();
                return;
            }

            Dictionary<string, Color?> initialColors = new Dictionary<string, Color?>(StringComparer.OrdinalIgnoreCase);
            foreach(string tag in parsedTags) {
                Color existing;
                if(TagManager.TryGetTagColor(tag, out existing) && existing != Color.Empty) {
                    initialColors[tag] = existing;
                }
                else {
                    initialColors[tag] = null;
                }
            }

            Dictionary<string, Color?> updatedColors;
            if(!TagColorEditorForm.TryEditColors(this, parsedTags, initialColors, out updatedColors)) {
                return;
            }

            try {
                TagManager.AddTags(targets, parsedTags);
                foreach(KeyValuePair<string, Color?> pair in updatedColors) {
                    TagManager.SetTagColor(pair.Key, pair.Value);
                }
                Close();
            }
            catch { }
        }

        private static IEnumerable<string> ParseTags(string text) {
            if(string.IsNullOrEmpty(text)) {
                yield break;
            }
            foreach(string token in text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
                string trimmed = token.Trim();
                if(trimmed.Length > 0) {
                    yield return trimmed;
                }
            }
        }

    }
}
