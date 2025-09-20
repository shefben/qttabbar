using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QTTabBarLib {
    internal static class OperationLedgerManager {
        private static readonly string Dir = Path.Combine(Path.GetTempPath(), "qttabbar_ops");
        private static readonly Dictionary<string, FileSystemWatcher> Watchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);

        public static void Watch(string path) {
            try {
                if (string.IsNullOrEmpty(path) || path.StartsWith("::")) return;
                if (!Directory.Exists(path)) return;
                if (Watchers.ContainsKey(path)) return;
                Directory.CreateDirectory(Dir);
                var w = new FileSystemWatcher(path) { IncludeSubdirectories = false, EnableRaisingEvents = true };
                w.Changed += (s,e) => Log(path, "CHANGED", e.FullPath);
                w.Created += (s,e) => Log(path, "CREATED", e.FullPath);
                w.Deleted += (s,e) => Log(path, "DELETED", e.FullPath);
                w.Renamed += (s,e) => Log(path, "RENAMED", e.OldFullPath + " -> " + e.FullPath);
                Watchers[path] = w;
            } catch { }
        }

        private static void Log(string basePath, string op, string detail) {
            try {
                string file = Path.Combine(Dir, DateTime.UtcNow.ToString("yyyyMMdd") + ".log");
                File.AppendAllText(file, string.Format("{0:O}\t{1}\t{2}\t{3}\n", DateTime.UtcNow, op, basePath, detail));
            } catch { }
        }

        public static List<string> LoadRecent(int days = 3, int max = 1000) {
            var list = new List<string>();
            try {
                var files = Enumerable.Range(0, days).Select(i => DateTime.UtcNow.AddDays(-i).ToString("yyyyMMdd") + ".log").Select(f => Path.Combine(Dir, f)).Where(File.Exists);
                foreach (var f in files) list.AddRange(File.ReadAllLines(f));
                if (list.Count > max) list = list.Skip(Math.Max(0, list.Count - max)).ToList();
            } catch { }
            return list;
        }
    }
}

