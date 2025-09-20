using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal static class SessionSnapshotManager {
        private static readonly string Dir = Path.Combine(Path.GetTempPath(), "qttabbar_snapshots");
        private static Timer timer;
        private const int DefaultMinutes = 5;
        private const int MaxSnapshots = 30;

        public class SnapshotTab { public string Path; public string Name; }
        public class Snapshot { public DateTime Utc; public List<SnapshotTab> Tabs = new List<SnapshotTab>(); }

        public static void EnsureStarted() {
            if (timer != null) return;
            try {
                Directory.CreateDirectory(Dir);
            } catch { }
            timer = new Timer { Interval = DefaultMinutes * 60 * 1000 };
            timer.Tick += (s, e) => SafeSnapshot();
            timer.Start();
        }

        public static void SafeSnapshot() {
            try { CreateSnapshot(); Prune(); } catch { }
        }

        public static void CreateSnapshot() {
            var snap = new Snapshot { Utc = DateTime.UtcNow };
            try {
                var bar = InstanceManager.GetThreadTabBar();
                if (bar != null) {
                    foreach (QTabItem t in bar.tabControl1.TabPages) snap.Tabs.Add(new SnapshotTab { Path = t.CurrentPath ?? string.Empty, Name = t.Text ?? string.Empty });
                }
            } catch { }
            var file = Path.Combine(Dir, "snap_" + snap.Utc.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".json");
            WriteJson(file, snap);
        }

        public static List<string> ListSnapshots() {
            try { return Directory.GetFiles(Dir, "snap_*.json").OrderByDescending(f => f).ToList(); } catch { return new List<string>(); }
        }

        public static void RestoreToCurrent(string file, QTTabBarClass bar) {
            try {
                var snap = ReadJson(file);
                if (snap == null || bar == null) return;
                // Open from snapshot (append without closing to respect protection levels)
                foreach (var t in snap.Tabs) {
                    using (var idl = new IDLWrapper(t.Path)) {
                        if (idl.Available) bar.OpenNewTab(idl, false, true);
                    }
                }
            } catch { }
        }

        private static void Prune() {
            try {
                var files = ListSnapshots();
                for (int i = MaxSnapshots; i < files.Count; i++) File.Delete(files[i]);
            } catch { }
        }

        private static void WriteJson(string file, Snapshot snap) {
            using (var sw = new StreamWriter(file, false)) {
                sw.Write("{\"utc\":\""); sw.Write(snap.Utc.ToString("o", CultureInfo.InvariantCulture)); sw.Write("\",\"tabs\":[");
                for (int i = 0; i < snap.Tabs.Count; i++) {
                    var t = snap.Tabs[i];
                    sw.Write("{\"path\":"); sw.Write(JsonEscape(t.Path ?? string.Empty)); sw.Write(",\"name\":"); sw.Write(JsonEscape(t.Name ?? string.Empty)); sw.Write('}');
                    if (i < snap.Tabs.Count - 1) sw.Write(',');
                }
                sw.Write("]}");
            }
        }

        private static Snapshot ReadJson(string file) {
            try {
                var text = File.ReadAllText(file);
                // super-minimal parse
                var tabs = new List<SnapshotTab>();
                int idxTabs = text.IndexOf("\"tabs\"");
                if (idxTabs > 0) {
                    int lb = text.IndexOf('[', idxTabs);
                    int rb = text.IndexOf(']', lb);
                    if (lb > 0 && rb > lb) {
                        var arr = text.Substring(lb + 1, rb - lb - 1).Split(new[] {"},"}, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in arr) {
                            string p = Extract(part + (part.EndsWith("}") ? "" : "}"), "path");
                            string n = Extract(part + (part.EndsWith("}") ? "" : "}"), "name");
                            tabs.Add(new SnapshotTab { Path = p ?? string.Empty, Name = n ?? string.Empty });
                        }
                    }
                }
                return new Snapshot { Utc = DateTime.UtcNow, Tabs = tabs };
            } catch { return null; }
        }

        private static string Extract(string obj, string key) {
            try {
                string pattern = "\"" + key + "\"";
                int i = obj.IndexOf(pattern, StringComparison.Ordinal);
                if (i < 0) return null; i = obj.IndexOf(':', i); if (i < 0) return null; i++;
                while (i < obj.Length && char.IsWhiteSpace(obj[i])) i++;
                if (obj[i] != '"') return null; i++;
                int start = i; var sb = new System.Text.StringBuilder(); bool esc = false;
                for (; i < obj.Length; i++) { char c = obj[i]; if (esc) { sb.Append(c); esc = false; continue; } if (c == '\\') { esc = true; continue; } if (c == '"') break; sb.Append(c);} return sb.ToString();
            } catch { return null; }
        }

        private static string JsonEscape(string s) {
            using (var sw = new StringWriter(CultureInfo.InvariantCulture)) {
                sw.Write('"');
                foreach (var ch in s) {
                    switch (ch) {
                        case '"': sw.Write("\\\""); break; case '\\': sw.Write("\\\\"); break; case '\b': sw.Write("\\b"); break; case '\f': sw.Write("\\f"); break; case '\n': sw.Write("\\n"); break; case '\r': sw.Write("\\r"); break; case '\t': sw.Write("\\t"); break; default: if (ch < 32) { sw.Write("\\u"); sw.Write(((int)ch).ToString("x4", System.Globalization.CultureInfo.InvariantCulture)); } else sw.Write(ch); break;
                    }
                }
                sw.Write('"'); return sw.ToString();
            }
        }
    }
}
