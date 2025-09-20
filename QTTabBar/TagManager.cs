using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QTTabBarLib {
    internal static class TagManager {
        private static readonly string FilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QTTabBar"), "tags.tsv");
        private static Dictionary<string, HashSet<string>> Map;
        private static readonly object Gate = new object();

        private static void Ensure() {
            if (Map != null) return;
            lock (Gate) {
                if (Map != null) return;
                Map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                try {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                    if (File.Exists(FilePath)) {
                        foreach (var line in File.ReadAllLines(FilePath)) {
                            var parts = line.Split('\t'); if (parts.Length < 2) continue;
                            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach (var t in parts[1].Split(',')) { var tt = (t ?? string.Empty).Trim(); if (tt.Length > 0) set.Add(tt); }
                            Map[parts[0]] = set;
                        }
                    }
                } catch { }
            }
        }

        public static void AddTags(IEnumerable<string> paths, IEnumerable<string> tags) {
            try {
                Ensure(); foreach (var p in paths) { HashSet<string> set; if (!Map.TryGetValue(p, out set)) Map[p] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (var t in tags) set.Add(t.Trim()); }
                Save();
            } catch { }
        }

        public static string GetTagSummary(string path) {
            Ensure(); HashSet<string> set; return Map.TryGetValue(path, out set) && set.Count>0 ? string.Join(",", set.OrderBy(x=>x).ToArray()) : string.Empty;
        }

        private static void Save() {
            try { var sb = new StringBuilder(); foreach (var kv in Map) sb.Append(kv.Key).Append('\t').AppendLine(string.Join(",", new List<string>(kv.Value).ToArray())); File.WriteAllText(FilePath, sb.ToString()); } catch { }
        }
        public static bool HighlightTagged { get; set; }
        public static bool DimUntagged { get; set; }
    }
}


