using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace QTTabBarLib {
    internal static class RecentOpenedTabsManager {
        private const string FileName = "qttabbar_recent_opened_tabs.json";

        private static string GetPath() {
            try { return Path.Combine(Path.GetTempPath(), FileName); } catch { return FileName; }
        }

        public class Entry {
            public string Path { get; set; }
            public string Name { get; set; }
            public DateTime AddedUtc { get; set; }
        }

        public static void Add(string path, string name) {
            if (IsNullOrWhiteSpaceCompat(path)) return;
            var list = Load();
            // De-duplicate by path (case-insensitive)
            list.RemoveAll(e => string.Equals(e.Path, path, StringComparison.OrdinalIgnoreCase));
            list.Insert(0, new Entry { Path = path, Name = name ?? string.Empty, AddedUtc = DateTime.UtcNow });

            int capacity = 15;
            try {
                // Reuse existing configurable history count if available
                capacity = Math.Max(1, QTTabBarLib.Config.Misc.TabHistoryCount);
            } catch { }
            if (list.Count > capacity) list = list.Take(capacity).ToList();
            Save(list);
        }

        public static List<Entry> GetAll() {
            return Load();
        }

        private static List<Entry> Load() {
            try {
                var path = GetPath();
                if (!File.Exists(path)) return new List<Entry>();
                var json = File.ReadAllText(path);
                return Deserialize(json) ?? new List<Entry>();
            } catch { return new List<Entry>(); }
        }

        private static void Save(List<Entry> entries) {
            try {
                var json = Serialize(entries ?? new List<Entry>());
                File.WriteAllText(GetPath(), json);
            } catch { }
        }

        // Minimal JSON (no external dependency)
        private static string Serialize(List<Entry> entries) {
            using (var sw = new StringWriter(CultureInfo.InvariantCulture)) {
                sw.Write('[');
                for (int i = 0; i < entries.Count; i++) {
                    var e = entries[i];
                    sw.Write('{');
                    sw.Write("\"path\":"); sw.Write(JsonEscape(e.Path ?? string.Empty)); sw.Write(',');
                    sw.Write("\"name\":"); sw.Write(JsonEscape(e.Name ?? string.Empty)); sw.Write(',');
                    sw.Write("\"addedAt\":\""); sw.Write(e.AddedUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)); sw.Write("\"");
                    sw.Write('}');
                    if (i < entries.Count - 1) sw.Write(',');
                }
                sw.Write(']');
                return sw.ToString();
            }
        }

        private static List<Entry> Deserialize(string json) {
            try {
                var list = new List<Entry>();
                if (IsNullOrWhiteSpaceCompat(json)) return list;
                // Super-lightweight parser for the specific format above
                // Fallback: naive split by '},{' to avoid pulling in JSON libs
                var trimmed = json.Trim();
                if (trimmed.Length < 2 || trimmed[0] != '[' || trimmed[trimmed.Length-1] != ']') return list;
                trimmed = trimmed.Substring(1, trimmed.Length - 2).Trim();
                if (trimmed.Length == 0) return list;
                var parts = SplitObjects(trimmed);
                foreach (var part in parts) {
                    var obj = part.Trim();
                    var path = ExtractString(obj, "\"path\"");
                    var name = ExtractString(obj, "\"name\"");
                    var added = ExtractString(obj, "\"addedAt\"");
                    DateTime addedUtc;
                    if (!DateTime.TryParseExact(added ?? string.Empty, "o", CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal, out addedUtc)) {
                        addedUtc = DateTime.UtcNow;
                    }
                    list.Add(new Entry { Path = path ?? string.Empty, Name = name ?? string.Empty, AddedUtc = addedUtc });
                }
                return list;
            } catch { return new List<Entry>(); }
        }

        private static List<string> SplitObjects(string s) {
            var list = new List<string>();
            int depth = 0; int start = 0;
            for (int i = 0; i < s.Length; i++) {
                char c = s[i];
                if (c == '{') depth++;
                else if (c == '}') depth--;
                if (depth == 0 && (c == '}' && (i+1 == s.Length || s[i+1] == ','))) {
                    int len = i - start + 1;
                    list.Add(s.Substring(start, len));
                    if (i+1 < s.Length && s[i+1] == ',') i++;
                    start = i+1;
                }
            }
            if (list.Count == 0) list.Add(s);
            return list;
        }

        private static string ExtractString(string obj, string key) {
            try {
                int i = obj.IndexOf(key, StringComparison.Ordinal);
                if (i < 0) return null;
                i = obj.IndexOf(':', i);
                if (i < 0) return null;
                i++;
                while (i < obj.Length && char.IsWhiteSpace(obj[i])) i++;
                if (i >= obj.Length || obj[i] != '"') return null;
                i++;
                int start = i;
                var sb = new System.Text.StringBuilder();
                bool esc = false;
                for (; i < obj.Length; i++) {
                    char c = obj[i];
                    if (esc) { sb.Append(c); esc = false; continue; }
                    if (c == '\\') { esc = true; continue; }
                    if (c == '"') break;
                    sb.Append(c);
                }
                return sb.ToString();
            } catch { return null; }
        }

        private static string JsonEscape(string s) {
            using (var sw = new StringWriter(CultureInfo.InvariantCulture)) {
                sw.Write('"');
                foreach (var ch in s) {
                    switch (ch) {
                        case '"': sw.Write("\\\""); break;
                        case '\\': sw.Write("\\\\"); break;
                        case '\b': sw.Write("\\b"); break;
                        case '\f': sw.Write("\\f"); break;
                        case '\n': sw.Write("\\n"); break;
                        case '\r': sw.Write("\\r"); break;
                        case '\t': sw.Write("\\t"); break;
                        default:
                            if (ch < 32) { sw.Write("\\u"); sw.Write(((int)ch).ToString("x4", CultureInfo.InvariantCulture)); }
                            else sw.Write(ch);
                            break;
                    }
                }
                sw.Write('"');
                return sw.ToString();
            }
        }

        private static bool IsNullOrWhiteSpaceCompat(string s) {
            return s == null || s.Trim().Length == 0;
        }
    }
}
