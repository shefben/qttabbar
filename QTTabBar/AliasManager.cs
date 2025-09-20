using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QTTabBarLib {
    internal static class AliasManager {
        private static readonly string FilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QTTabBar"), "aliases.txt");
        private static Dictionary<string,string> Map;
        private static readonly object Gate = new object();

        private static void EnsureLoaded() {
            if (Map != null) return;
            lock (Gate) {
                if (Map != null) return;
                Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                try {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                    if (File.Exists(FilePath)) {
                        foreach (var line in File.ReadAllLines(FilePath)) {
                            int eq = line.IndexOf('='); if (eq <= 0) continue;
                            string k = line.Substring(0, eq).Trim(); string v = line.Substring(eq+1).Trim();
                            if (!k.StartsWith("@")) k = "@" + k;
                            Map[k] = v;
                        }
                    }
                } catch { }
            }
        }

        public static string Expand(string path) {
            try {
                EnsureLoaded(); if (string.IsNullOrEmpty(path)) return path;
                foreach (var kv in Map) {
                    if (path.StartsWith(kv.Key + "\\", StringComparison.OrdinalIgnoreCase) || string.Equals(path, kv.Key, StringComparison.OrdinalIgnoreCase))
                        return kv.Value + path.Substring(kv.Key.Length);
                }
                return path;
            } catch { return path; }
        }

        public static void Set(string alias, string target) {
            try {
                EnsureLoaded(); if (!alias.StartsWith("@")) alias = "@" + alias;
                Map[alias] = target; Save();
            } catch { }
        }

        private static void Save() {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var sb = new StringBuilder();
                foreach (var kv in Map) sb.Append(kv.Key).Append('=').AppendLine(kv.Value);
                File.WriteAllText(FilePath, sb.ToString());
            } catch { }
        }
    }
}
