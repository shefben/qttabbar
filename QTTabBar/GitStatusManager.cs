using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace QTTabBarLib {
    internal static class GitStatusManager {
        private static readonly Dictionary<string, RepoInfo> Cache = new Dictionary<string, RepoInfo>(StringComparer.OrdinalIgnoreCase);
        private static readonly object CacheGate = new object();
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);

        private class RepoInfo {
            public string Branch;
            public int Modified;
            public int Untracked;
            public DateTime LastUpdatedUtc;
        }

        public static void UpdateTabBadge(QTTabBarClass bar, QTabItem tab) {
            if (bar == null || tab == null) return;
            string path = tab.CurrentPath;
            if (string.IsNullOrEmpty(path) || path.StartsWith("::")) return;
            ThreadPool.QueueUserWorkItem(_ => {
                try {
                    var info = GetRepoInfo(path);
                    if (info == null) return;
                    string badge = FormatBadge(info);
                    if (string.IsNullOrEmpty(badge)) return;
                    bar.BeginInvoke(new Action(() => {
                        try {
                            if (tab.CurrentPath == path) {
                                if (!tab.Text.Contains(badge)) {
                                    tab.Text = tab.Text + " " + badge;
                                }
                            }
                        } catch { }
                    }));
                } catch { }
            });
        }

        private static string FormatBadge(RepoInfo info) {
            var sb = new StringBuilder();
            sb.Append('[').Append(info.Branch ?? "?");
            if (info.Modified > 0) sb.Append('*').Append(info.Modified);
            if (info.Untracked > 0) sb.Append('+').Append(info.Untracked);
            sb.Append(']');
            return sb.ToString();
        }

        private static RepoInfo GetRepoInfo(string path) {
            string root = FindGitRoot(path);
            if (root == null) return null;
            RepoInfo info;
            lock (CacheGate) {
                if (Cache.TryGetValue(root, out info)) {
                    if (DateTime.UtcNow - info.LastUpdatedUtc < RefreshInterval) return info;
                }
            }
            info = QueryGit(root) ?? new RepoInfo { Branch = null, Modified = 0, Untracked = 0, LastUpdatedUtc = DateTime.UtcNow };
            lock (CacheGate) { Cache[root] = info; }
            return info;
        }

        private static RepoInfo QueryGit(string root) {
            try {
                string branch = RunGit(root, "rev-parse --abbrev-ref HEAD").Trim();
                string status = RunGit(root, "status --porcelain");
                int modified = 0, untracked = 0;
                using (var sr = new StringReader(status)) {
                    string line;
                    while ((line = sr.ReadLine()) != null) {
                        if (line.StartsWith("??")) untracked++;
                        else modified++;
                    }
                }
                return new RepoInfo { Branch = string.IsNullOrEmpty(branch) ? "?" : branch, Modified = modified, Untracked = untracked, LastUpdatedUtc = DateTime.UtcNow };
            } catch { return null; }
        }

        private static string RunGit(string workdir, string args) {
            var psi = new ProcessStartInfo {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = workdir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var p = Process.Start(psi)) {
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(2000);
                return output ?? string.Empty;
            }
        }

        private static string FindGitRoot(string path) {
            try {
                var dir = new DirectoryInfo(path);
                if (!dir.Exists) return null;
                while (dir != null) {
                    if (Directory.Exists(Path.Combine(dir.FullName, ".git"))) return dir.FullName;
                    dir = dir.Parent;
                }
                return null;
            } catch { return null; }
        }
    }
}
