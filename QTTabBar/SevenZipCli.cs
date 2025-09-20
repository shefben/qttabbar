using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace QTTabBarLib {
    internal static class SevenZipCli {
        private static string cachedPath;

        public static bool IsAvailable() {
            if (cachedPath != null) return cachedPath.Length > 0;
            cachedPath = Find7z();
            return cachedPath.Length > 0;
        }

        public static bool Supports(string archivePath) {
            string ext = Path.GetExtension(archivePath).ToLowerInvariant();
            return (ext == ".7z" || ext == ".rar" || ext == ".zip") && IsAvailable();
        }

        public static List<string> ListEntries(string archivePath) {
            var list = new List<string>();
            try {
                if (!Supports(archivePath)) return list;
                // Use -slt to get detailed key=value lines
                string output = Run(cachedPath, "l -slt -- \"" + archivePath + "\"");
                using (var sr = new StringReader(output)) {
                    string line; string path = null;
                    while ((line = sr.ReadLine()) != null) {
                        if (line.StartsWith("Path = ")) {
                            path = line.Substring(7).Trim();
                            if (path.Length > 0) list.Add(path);
                        }
                    }
                }
            } catch { }
            return list;
        }

        public static bool Extract(string archivePath, string destFolder, IEnumerable<string> entryNames) {
            try {
                if (!Supports(archivePath)) return false;
                Directory.CreateDirectory(destFolder);
                var args = new StringBuilder();
                args.Append("x -y -- ")
                    .Append('"').Append(archivePath).Append('"')
                    .Append(" -o\"").Append(destFolder).Append("\"");
                // If specific entries selected, append them
                if (entryNames != null) {
                    foreach (var e in entryNames) {
                        if (!string.IsNullOrEmpty(e)) {
                            args.Append(' ').Append('"').Append(e).Append('"');
                        }
                    }
                }
                Run(cachedPath, args.ToString());
                return true;
            } catch { return false; }
        }

        private static string Run(string exe, string args) {
            var psi = new ProcessStartInfo {
                FileName = exe,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            using (var p = Process.Start(psi)) {
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(15000);
                return output ?? string.Empty;
            }
        }

        private static string Find7z() {
            try {
                // 1) PATH
                string[] pathDirs = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(';');
                foreach (var d in pathDirs) {
                    try { var f = Path.Combine(d.Trim().Trim('"'), "7z.exe"); if (File.Exists(f)) return f; } catch { }
                }
                // 2) Common install locations (support .NET 3.5: no ProgramFilesX86)
                string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string pf86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                var candidates = new List<string>();
                candidates.Add(Path.Combine(Path.Combine(pf, "7-Zip"), "7z.exe"));
                if (!string.IsNullOrEmpty(pf86)) candidates.Add(Path.Combine(Path.Combine(pf86, "7-Zip"), "7z.exe"));
                foreach (var c in candidates) if (File.Exists(c)) return c;
            } catch { }
            return string.Empty;
        }
    }
}
