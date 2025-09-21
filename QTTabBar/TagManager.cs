using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace QTTabBarLib {
    internal static class TagManager {
        private static readonly string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QTTabBar");
        private static readonly string TagsFilePath = Path.Combine(DirectoryPath, "tags.tsv");
        private static readonly string TagDefinitionsPath = Path.Combine(DirectoryPath, "tagdefs.tsv");

        private static readonly object Gate = new object();

        private static Dictionary<string, HashSet<string>> tagAssignments;
        private static Dictionary<string, Color> tagColors;

        public static bool HighlightTagged { get; set; }
        public static bool DimUntagged { get; set; }

        private static void Ensure() {
            if(tagAssignments != null) return;
            lock(Gate) {
                if(tagAssignments != null) return;
                tagAssignments = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                tagColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
                try {
                    Directory.CreateDirectory(DirectoryPath);
                }
                catch { }
                LoadAssignments();
                LoadDefinitions();
            }
        }

        private static void LoadAssignments() {
            try {
                if(!File.Exists(TagsFilePath)) {
                    return;
                }

                foreach(string line in File.ReadAllLines(TagsFilePath)) {
                    if(string.IsNullOrWhiteSpace(line)) {
                        continue;
                    }
                    string[] parts = line.Split('	');
                    if(parts.Length < 2) {
                        continue;
                    }
                    string path = parts[0];
                    HashSet<string> tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach(string token in parts[1].Split(',')) {
                        string tag = (token ?? string.Empty).Trim();
                        if(tag.Length > 0) {
                            tags.Add(tag);
                        }
                    }
                    tagAssignments[path] = tags;
                }
            }
            catch { }
        }

        private static void LoadDefinitions() {
            try {
                if(!File.Exists(TagDefinitionsPath)) {
                    return;
                }
                foreach(string line in File.ReadAllLines(TagDefinitionsPath)) {
                    if(string.IsNullOrWhiteSpace(line)) {
                        continue;
                    }
                    string[] parts = line.Split('	');
                    if(parts.Length < 2) {
                        continue;
                    }
                    Color color;
                    if(TryParseColor(parts[1], out color)) {
                        tagColors[parts[0]] = color;
                    }
                }
            }
            catch { }
        }

        public static void AddTags(IEnumerable<string> paths, IEnumerable<string> tags) {
            if(paths == null || tags == null) {
                return;
            }
            Ensure();
            bool changed = false;
            foreach(string path in paths.Where(p => !string.IsNullOrWhiteSpace(p))) {
                HashSet<string> set;
                if(!tagAssignments.TryGetValue(path, out set)) {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    tagAssignments[path] = set;
                }
                foreach(string tag in tags) {
                    string trimmed = (tag ?? string.Empty).Trim();
                    if(trimmed.Length == 0) {
                        continue;
                    }
                    if(set.Add(trimmed)) {
                        changed = true;
                    }
                }
            }
            if(changed) {
                SaveAssignments();
                BroadcastTagChanges();
            }
        }

        public static string GetTagSummary(string path) {
            Ensure();
            if(string.IsNullOrEmpty(path)) {
                return string.Empty;
            }
            HashSet<string> set;
            if(tagAssignments.TryGetValue(path, out set) && set.Count > 0) {
                return string.Join(",", set.OrderBy(x => x));
            }
            return string.Empty;
        }

        public static Color? GetTagColorForPath(string path) {
            Ensure();
            if(string.IsNullOrEmpty(path)) {
                return null;
            }
            HashSet<string> set;
            if(!tagAssignments.TryGetValue(path, out set) || set.Count == 0) {
                return null;
            }
            foreach(string tag in set.OrderBy(tag => tag)) {
                Color color;
                if(tagColors.TryGetValue(tag, out color)) {
                    return color;
                }
            }
            return null;
        }

        public static bool TryGetTagColor(string tag, out Color color) {
            Ensure();
            if(tag != null && tagColors.TryGetValue(tag, out color)) {
                return true;
            }
            color = Color.Empty;
            return false;
        }

        public static void SetTagColor(string tag, Color? color) {
            if(string.IsNullOrWhiteSpace(tag)) {
                return;
            }
            Ensure();
            bool changed = false;
            if(color.HasValue) {
                Color existing;
                if(!tagColors.TryGetValue(tag, out existing) || existing != color.Value) {
                    tagColors[tag] = color.Value;
                    changed = true;
                }
            }
            else if(tagColors.Remove(tag)) {
                changed = true;
            }
            if(changed) {
                SaveDefinitions();
                BroadcastTagChanges();
            }
        }

        private static void SaveAssignments() {
            try {
                StringBuilder builder = new StringBuilder();
                foreach(KeyValuePair<string, HashSet<string>> pair in tagAssignments) {
                    if(pair.Value.Count == 0) {
                        continue;
                    }
                    builder.Append(pair.Key).Append('	').Append(string.Join(",", pair.Value.OrderBy(tag => tag))).AppendLine();
                }
                File.WriteAllText(TagsFilePath, builder.ToString());
            }
            catch { }
        }

        private static void SaveDefinitions() {
            try {
                StringBuilder builder = new StringBuilder();
                foreach(KeyValuePair<string, Color> pair in tagColors.OrderBy(kv => kv.Key)) {
                    builder.Append(pair.Key).Append('	').Append(ColorTranslator.ToHtml(pair.Value)).AppendLine();
                }
                File.WriteAllText(TagDefinitionsPath, builder.ToString());
            }
            catch { }
        }

        private static bool TryParseColor(string value, out Color color) {
            color = Color.Empty;
            if(string.IsNullOrWhiteSpace(value)) {
                return false;
            }
            value = value.Trim();
            try {
                color = ColorTranslator.FromHtml(value);
                return true;
            }
            catch {
                if(value.Length == 6) {
                    int rgb;
                    if(int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb)) {
                        color = Color.FromArgb(255, (rgb >> 16) & 0xff, (rgb >> 8) & 0xff, rgb & 0xff);
                        return true;
                    }
                }
            }
            return false;
        }

        private static void BroadcastTagChanges() {
            try {
                InstanceManager.TabBarBroadcast(tabBar => tabBar.RefreshTagVisuals(), true);
            }
            catch { }
        }
    }
}
