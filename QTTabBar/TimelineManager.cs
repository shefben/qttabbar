using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace QTTabBarLib {
    internal static class TimelineManager {
        private static readonly string FilePath = Path.Combine(Path.GetTempPath(), "qttabbar_timeline.jsonl");

        public class Entry { public DateTime Utc; public string Path; }

        public static void RecordNavigation(string path) {
            if (string.IsNullOrEmpty(path)) return;
            try {
                var line = string.Format(CultureInfo.InvariantCulture, "{0:o}\t{1}\n", DateTime.UtcNow, path.Replace("\t", " "));
                File.AppendAllText(FilePath, line);
            } catch { }
        }

        public static List<Entry> Load(int max = 1000) {
            var list = new List<Entry>();
            try {
                if (!File.Exists(FilePath)) return list;
                var lines = File.ReadAllLines(FilePath);
                for (int i = lines.Length - 1; i >= 0 && list.Count < max; i--) {
                    var line = lines[i];
                    int tab = line.IndexOf('\t');
                    if (tab <= 0) continue;
                    DateTime dt;
                    if (!DateTime.TryParseExact(line.Substring(0, tab), "o", CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal, out dt)) continue;
                    var path = line.Substring(tab + 1);
                    list.Add(new Entry { Utc = dt, Path = path });
                }
            } catch { }
            return list;
        }
    }
}

