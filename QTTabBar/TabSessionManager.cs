using System;
using System.Diagnostics;
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
        private static readonly string SessionDirectory = InitializeSessionDirectory();
        private static readonly string LegacyDirectory = Path.GetTempPath();
        private const string SessionPattern = "qttabbar_session_*.json";
        private static readonly object FileLock = new object();

        private static WindowSession GetSession(IntPtr hwnd) {
            WindowSession ws;
            if(!Sessions.TryGetValue(hwnd, out ws)) {
                ws = new WindowSession();
                Sessions[hwnd] = ws;
            }
            return ws;
        }

        private static string GetSessionPath(IntPtr hwnd) {
            string name = BuildSessionFileName(hwnd);
            return Path.Combine(SessionDirectory, name);
        }

        private static string GetLegacySessionPath(IntPtr hwnd) {
            string name = BuildSessionFileName(hwnd);
            return Path.Combine(LegacyDirectory, name);
        }

        public static void SaveFor(QTTabBarClass bar) {
            if(bar == null) return;
            try { if (bar.HideExplorer) return; } catch { }
            var hwnd = bar.GetExplorerHandle();
            if(hwnd == IntPtr.Zero) return;

            var session = GetSession(hwnd);

            var tabs = bar.tabControl1.TabPages.Cast<QTabItem>().ToList();
            var now = DateTime.UtcNow;
            foreach(var t in tabs) {
                if(!session.OpenTimes.ContainsKey(t)) {
                    session.OpenTimes[t] = now;
                }
            }
            var keys = session.OpenTimes.Keys.ToList();
            foreach(var k in keys) {
                if(!tabs.Contains(k)) {
                    session.OpenTimes.Remove(k);
                }
            }

            try {
                string path = GetSessionPath(hwnd);
                lock(FileLock) {
                    string dir = Path.GetDirectoryName(path);
                    if(!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    using(var sw = new StreamWriter(path, false)) {
                        sw.Write('[');
                        for(int i = 0; i < tabs.Count; i++) {
                            var t = tabs[i];
                            DateTime openedAt;
                            if(!session.OpenTimes.TryGetValue(t, out openedAt)) openedAt = DateTime.UtcNow;
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
            } catch { }
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

        public sealed class SavedSession {

            public string FilePath { get; internal set; }

            public DateTime LastWriteTimeUtc { get; internal set; }

            public SavedTab[] Tabs { get; internal set; }

            public DateTime LastWriteTimeLocal { get { return LastWriteTimeUtc.ToLocalTime(); } }

            public int TabCount { get { return Tabs == null ? 0 : Tabs.Length; } }

        }



        public static SavedTab[] LoadForWindowOrLatest(IntPtr hwnd) {
            try {
                string exact = GetSessionPath(hwnd);
                if(File.Exists(exact)) {
                    return Deserialize(File.ReadAllText(exact));
                }

                string legacy = GetLegacySessionPath(hwnd);
                if(File.Exists(legacy)) {
                    var tabs = Deserialize(File.ReadAllText(legacy));
                    TryCopyLegacyFile(legacy, exact);
                    return tabs;
                }

                var candidates = EnumerateSessionFiles().Select(f => new FileInfo(f)).OrderByDescending(fi => fi.LastWriteTimeUtc).ToList();
                if(candidates.Count == 0) return new SavedTab[0];
                return Deserialize(File.ReadAllText(candidates[0].FullName));
            } catch { return new SavedTab[0]; }
        }

        private static string BuildSessionFileName(IntPtr hwnd) {
            return "qttabbar_session_" + ((long)hwnd).ToString("X", CultureInfo.InvariantCulture) + ".json";
        }

        private static string InitializeSessionDirectory() {
            try {
                string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if(string.IsNullOrEmpty(baseDir)) baseDir = Path.GetTempPath();
                string dir = Path.Combine(Path.Combine(baseDir, "QTTabBar"), "Sessions");
                Directory.CreateDirectory(dir);
                return dir;
            } catch {
                try {
                    string fallback = Path.Combine(Path.Combine(Path.GetTempPath(), "QTTabBar"), "Sessions");
                    Directory.CreateDirectory(fallback);
                    return fallback;
                } catch {
                    return Path.GetTempPath();
                }
            }
        }

        private static IEnumerable<string> EnumerateSessionFiles() {

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string[] roots = new[] { SessionDirectory, LegacyDirectory };

            for(int i = 0; i < roots.Length; i++) {

                string root = roots[i];

                if(string.IsNullOrEmpty(root)) continue;

                string[] files = null;

                try {

                    if(Directory.Exists(root)) {

                        files = Directory.GetFiles(root, SessionPattern);

                    }

                } catch { }

                if(files == null || files.Length == 0) continue;

                for(int j = 0; j < files.Length; j++) {

                    string file = files[j];

                    string full;

                    try {

                        full = Path.GetFullPath(file);

                    } catch {

                        continue;

                    }

                    if(seen.Add(full)) {

                        yield return full;

                    }

                }

            }

        }











        private static void TryCopyLegacyFile(string source, string target) {
            try {
                string sourceFull = Path.GetFullPath(source);
                string targetFull = Path.GetFullPath(target);
                if(string.Equals(sourceFull, targetFull, StringComparison.OrdinalIgnoreCase)) return;
            } catch { }
            try {
                string dir = Path.GetDirectoryName(target);
                if(!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.Copy(source, target, true);
            } catch { }
        }

        public static List<SavedSession> GetSavedSessions() {

            var result = new List<SavedSession>();

            foreach(string file in EnumerateSessionFiles()) {

                try {

                    SavedTab[] tabs = Deserialize(File.ReadAllText(file));

                    if(tabs == null || tabs.Length == 0) continue;

                    DateTime lastWrite;

                    try { lastWrite = File.GetLastWriteTimeUtc(file); }

                    catch { lastWrite = DateTime.UtcNow; }

                    result.Add(new SavedSession {

                        FilePath = file,

                        LastWriteTimeUtc = lastWrite,

                        Tabs = tabs

                    });

                } catch { }

            }

            return result.OrderByDescending(r => r.LastWriteTimeUtc).ToList();

        }



        public static bool LaunchSession(SavedSession session) {

            if(session == null) return false;

            return LaunchSession(session.Tabs);

        }



        public static bool LaunchSession(IEnumerable<SavedTab> tabs) {

            if(tabs == null) return false;

            var distinct = new List<string>();

            var comparer = StringComparer.OrdinalIgnoreCase;

            foreach(var tab in tabs) {

                string path = tab == null ? null : tab.Path;

                if(string.IsNullOrEmpty(path)) continue;

                if(path.StartsWith("::", StringComparison.OrdinalIgnoreCase)) continue;

                if(distinct.Any(existing => comparer.Equals(existing, path))) continue;

                distinct.Add(path);

            }

            if(distinct.Count == 0) return false;

            int firstValidIndex = -1;

            for(int i = 0; i < distinct.Count; i++) {

                try {

                    using(IDLWrapper idl = new IDLWrapper(distinct[i])) {

                        if(idl.Available && idl.HasPath && idl.IsFolder && !idl.IsLinkToDeadFolder && idl.IsReadyIfDrive) {

                            firstValidIndex = i;

                            break;

                        }

                    }

                } catch { }

            }

            if(firstValidIndex == -1) return false;

            string firstPath = distinct[firstValidIndex];

            if(firstValidIndex > 0) {

                distinct.RemoveAt(firstValidIndex);

                distinct.Insert(0, firstPath);

            }

            try {

                QTUtility2.InitializeTemporaryPaths();

                StaticReg.CreateWindowPaths.Assign(distinct.ToArray());

                var psi = new ProcessStartInfo {

                    FileName = "explorer.exe",

                    Arguments = "\"" + firstPath + "\"",

                    UseShellExecute = true

                };

                Process.Start(psi);

                return true;

            } catch {

                try {

                    Process.Start(firstPath);

                    return true;

                } catch { }

            }

            return false;

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
            using(var sw = new StringWriter(CultureInfo.InvariantCulture)) {
                sw.Write('"');
                if(!string.IsNullOrEmpty(s)) {
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
                }
                sw.Write('"');
                return sw.ToString();
            }
        }
    }
}
