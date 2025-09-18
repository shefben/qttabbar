using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal static class TabSessionManager {
        private class WindowSession {
            public Dictionary<QTabItem, DateTime> OpenTimes = new Dictionary<QTabItem, DateTime>();
        }

        private static readonly Dictionary<IntPtr, WindowSession> Sessions = new Dictionary<IntPtr, WindowSession>();

        private static WindowSession GetSession(IntPtr hwnd) {
            WindowSession ws;
            if(!Sessions.TryGetValue(hwnd, out ws)) {
                ws = new WindowSession();
                Sessions[hwnd] = ws;
            }
            return ws;
        }

        private static string GetSessionPath(IntPtr hwnd) {
            string temp = Path.GetTempPath();
            string name = "qttabbar_session_" + ((long)hwnd).ToString("X", CultureInfo.InvariantCulture) + ".json";
            return Path.Combine(temp, name);
        }

        public static void SaveFor(QTTabBarClass bar) {
            if(bar == null) return;
            // ExplorerHandle may not be ready during early init
            var hwnd = bar.GetExplorerHandle();
            if(hwnd == IntPtr.Zero) return;

            var session = GetSession(hwnd);

            // Build snapshot of current tabs
            var tabs = bar.tabControl1.TabPages.Cast<QTabItem>().ToList();
            var now = DateTime.UtcNow;
            foreach(var t in tabs) {
                if(!session.OpenTimes.ContainsKey(t)) {
                    session.OpenTimes[t] = now;
                }
            }
            // Remove disposed tabs
            var keys = session.OpenTimes.Keys.ToList();
            foreach(var k in keys) {
                if(!tabs.Contains(k)) {
                    session.OpenTimes.Remove(k);
                }
            }

            // Serialize minimal JSON manually to avoid dependencies
            // [{"order":0,"name":"...","path":"...","openedAt":"2025-01-01T12:34:56Z"}, ...]
            using(var sw = new StreamWriter(GetSessionPath(hwnd), false)) {
                sw.Write('[');
                for(int i = 0; i < tabs.Count; i++) {
                    var t = tabs[i];
                    var openedAt = session.OpenTimes[t];
                    sw.Write('{');
                    sw.Write("\"order\":"); sw.Write(i.ToString(CultureInfo.InvariantCulture));
                    sw.Write(',');
                    sw.Write("\"name\":"); sw.Write(JsonEscape(t.Text ?? string.Empty));
                    sw.Write(',');
                    sw.Write("\"path\":"); sw.Write(JsonEscape(t.CurrentPath ?? string.Empty));
                    sw.Write(',');
                    sw.Write("\"openedAt\":\""); sw.Write(openedAt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)); sw.Write("\"");
                    sw.Write('}');
                    if(i < tabs.Count - 1) sw.Write(',');
                }
                sw.Write(']');
            }
        }

        [DataContract]
        public class SavedTab {
            [DataMember(Name = "order")] public int Order { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "path")] public string Path { get; set; }
            [DataMember(Name = "openedAt")] public string OpenedAtIso { get; set; }
            public DateTime OpenedAtUtc {
                get {
                    DateTime dt;
                    if(DateTime.TryParseExact(OpenedAtIso ?? string.Empty, "o", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dt)) return dt;
                    return DateTime.UtcNow;
                }
            }
        }

        public static SavedTab[] LoadForWindowOrLatest(IntPtr hwnd) {
            try {
                string exact = GetSessionPath(hwnd);
                if(File.Exists(exact)) {
                    return Deserialize(File.ReadAllText(exact));
                }
                string temp = Path.GetTempPath();
                var files = Directory.GetFiles(temp, "qttabbar_session_*.json");
                if(files.Length == 0) return new SavedTab[0];
                string latest = files.Select(f => new FileInfo(f)).OrderByDescending(fi => fi.LastWriteTimeUtc).First().FullName;
                return Deserialize(File.ReadAllText(latest));
            } catch { return new SavedTab[0]; }
        }

        private static SavedTab[] Deserialize(string json) {
            if(string.IsNullOrEmpty(json)) return new SavedTab[0];
            try {
                using(var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json))) {
                    var ser = new DataContractJsonSerializer(typeof(SavedTab[]));
                    var arr = ser.ReadObject(ms) as SavedTab[];
                    return arr ?? new SavedTab[0];
                }
            } catch { return new SavedTab[0]; }
        }

        public static void SetOpenTime(QTTabBarClass bar, QTabItem tab, DateTime openedUtc) {
            if(bar == null || tab == null) return;
            var session = GetSession(bar.GetExplorerHandle());
            session.OpenTimes[tab] = openedUtc;
        }

        private static string JsonEscape(string s) {
            if(s == null) return "\"\"";
            using(var sw = new StringWriter(CultureInfo.InvariantCulture)) {
                sw.Write('"');
                foreach(var ch in s) {
                    switch(ch) {
                        case '"': sw.Write("\\\""); break;
                        case '\\': sw.Write("\\\\"); break;
                        case '\b': sw.Write("\\b"); break;
                        case '\f': sw.Write("\\f"); break;
                        case '\n': sw.Write("\\n"); break;
                        case '\r': sw.Write("\\r"); break;
                        case '\t': sw.Write("\\t"); break;
                        default:
                            if(ch < 32) {
                                sw.Write("\\u");
                                sw.Write(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                            } else {
                                sw.Write(ch);
                            }
                            break;
                    }
                }
                sw.Write('"');
                return sw.ToString();
            }
        }
    }
}
