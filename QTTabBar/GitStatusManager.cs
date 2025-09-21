using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Text;
using System.Threading;

namespace QTTabBarLib {
    internal static class GitStatusManager {
        private sealed class RepoInfo {
            public string Branch;
            public int Modified;
            public int Untracked;
            public GitStatusKind Status;
            public DateTime LastUpdatedUtc;
        }

        private static readonly Dictionary<string, RepoInfo> Cache = new Dictionary<string, RepoInfo>(StringComparer.OrdinalIgnoreCase);
        private static readonly object CacheGate = new object();
        private static readonly object IconGate = new object();
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);

        public static void UpdateTabBadge(QTTabBarClass bar, QTabItem tab) {
            if(bar == null || tab == null) {
                return;
            }
            string path = tab.CurrentPath;
            if(string.IsNullOrEmpty(path) || path.StartsWith("::", StringComparison.Ordinal)) {
                return;
            }
            ThreadPool.QueueUserWorkItem(_ => {
                RepoInfo info = null;
                try {
                    info = GetRepoInfo(path);
                }
                catch {
                    info = null;
                }
                GitStatusKind status = info != null ? info.Status : GitStatusKind.None;
                string branch = info != null ? info.Branch : null;
                bar.BeginInvoke(new Action(() => {
                    try {
                        if(tab.CurrentPath != path) {
                            return;
                        }
                        string overlayKey = null;
                        if(status != GitStatusKind.None) {
                            overlayKey = EnsureOverlay(tab.GetBaseImageKey(), status);
                        }
                        tab.SetGitStatus(status, overlayKey);
                        UpdateTooltip(tab, branch, status);
                    }
                    catch { }
                }));
            });
        }

        private static void UpdateTooltip(QTabItem tab, string branch, GitStatusKind status) {
            if(tab == null) {
                return;
            }
            string tooltip = tab.ToolTipText;
            if(status == GitStatusKind.None || string.IsNullOrEmpty(branch)) {
                tab.ToolTipText = tooltip;
                return;
            }
            string badge = FormatBadge(branch, status);
            if(string.IsNullOrEmpty(tooltip)) {
                tab.ToolTipText = badge;
            }
            else if(tooltip.IndexOf(badge, StringComparison.OrdinalIgnoreCase) < 0) {
                tab.ToolTipText = tooltip + Environment.NewLine + badge;
            }
        }

        private static string FormatBadge(string branch, GitStatusKind status) {
            var sb = new StringBuilder();
            sb.Append("Git: ").Append(branch ?? "?");
            sb.Append(" (" + status + ")");
            return sb.ToString();
        }

        private static RepoInfo GetRepoInfo(string path) {
            string root = FindGitRoot(path);
            if(root == null) {
                return null;
            }
            RepoInfo info;
            lock(CacheGate) {
                if(Cache.TryGetValue(root, out info)) {
                    if(DateTime.UtcNow - info.LastUpdatedUtc < RefreshInterval) {
                        return info;
                    }
                }
            }
            info = QueryGit(root) ?? new RepoInfo { Branch = null, Modified = 0, Untracked = 0, Status = GitStatusKind.Unknown, LastUpdatedUtc = DateTime.UtcNow };
            lock(CacheGate) {
                Cache[root] = info;
            }
            return info;
        }

        private static RepoInfo QueryGit(string root) {
            try {
                string branch = RunGit(root, "rev-parse --abbrev-ref HEAD").Trim();
                string status = RunGit(root, "status --porcelain");
                int modified = 0;
                int untracked = 0;
                using(StringReader reader = new StringReader(status)) {
                    string line;
                    while((line = reader.ReadLine()) != null) {
                        if(line.StartsWith("??", StringComparison.Ordinal)) {
                            untracked++;
                        }
                        else if(line.Length > 0) {
                            modified++;
                        }
                    }
                }
                RepoInfo info = new RepoInfo {
                    Branch = string.IsNullOrEmpty(branch) ? "?" : branch,
                    Modified = modified,
                    Untracked = untracked,
                    Status = DetermineStatus(modified, untracked),
                    LastUpdatedUtc = DateTime.UtcNow
                };
                return info;
            }
            catch {
                return null;
            }
        }

        private static GitStatusKind DetermineStatus(int modified, int untracked) {
            if(modified == 0 && untracked == 0) {
                return GitStatusKind.Clean;
            }
            if(modified > 0 && untracked > 0) {
                return GitStatusKind.Mixed;
            }
            if(modified > 0) {
                return GitStatusKind.Modified;
            }
            if(untracked > 0) {
                return GitStatusKind.Untracked;
            }
            return GitStatusKind.Unknown;
        }

        private static string EnsureOverlay(string baseKey, GitStatusKind status) {
            if(string.IsNullOrEmpty(baseKey) || QTUtility.ImageListGlobal == null) {
                return baseKey;
            }
            string overlayKey = baseKey + "|git:" + status;
            if(QTUtility.ImageListGlobal.Images.ContainsKey(overlayKey)) {
                return overlayKey;
            }
            lock(IconGate) {
                if(QTUtility.ImageListGlobal.Images.ContainsKey(overlayKey)) {
                    return overlayKey;
                }
                Image baseImage = QTUtility.ImageListGlobal.Images.ContainsKey(baseKey)
                        ? QTUtility.ImageListGlobal.Images[baseKey]
                        : null;
                if(baseImage == null) {
                    return baseKey;
                }
                using(Bitmap composed = ComposeOverlay(baseImage, status)) {
                    if(composed != null) {
                        QTUtility.ImageListGlobal.Images.Add(overlayKey, composed);
                    }
                }
            }
            return overlayKey;
        }

        private static Bitmap ComposeOverlay(Image baseImage, GitStatusKind status) {
            int width = baseImage.Width;
            int height = baseImage.Height;
            if(width <= 0 || height <= 0) {
                return null;
            }
            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using(Graphics g = Graphics.FromImage(result)) {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawImage(baseImage, new Rectangle(0, 0, width, height));

                int overlaySize = Math.Max(6, Math.Min(width, height) / 2);
                Rectangle overlayRect = new Rectangle(width - overlaySize, height - overlaySize, overlaySize, overlaySize);
                Color fill = GetStatusColor(status);
                using(SolidBrush brush = new SolidBrush(fill)) {
                    g.FillEllipse(brush, overlayRect);
                }
                using(Pen border = new Pen(Color.White, Math.Max(1f, overlaySize / 6f))) {
                    g.DrawEllipse(border, overlayRect);
                }
                string glyph = GetStatusGlyph(status);
                if(!string.IsNullOrEmpty(glyph)) {
                    using(Font font = new Font("Segoe UI Symbol", Math.Max(overlayRect.Width - 1, 6), FontStyle.Bold, GraphicsUnit.Pixel))
                    using(SolidBrush glyphBrush = new SolidBrush(Color.White)) {
                        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                        StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(glyph, font, glyphBrush, overlayRect, format);
                    }
                }
            }
            return result;
        }

        private static Color GetStatusColor(GitStatusKind status) {
            switch(status) {
                case GitStatusKind.Clean:
                    return Color.FromArgb(76, 175, 80);
                case GitStatusKind.Modified:
                    return Color.FromArgb(255, 152, 0);
                case GitStatusKind.Untracked:
                    return Color.FromArgb(33, 150, 243);
                case GitStatusKind.Mixed:
                    return Color.FromArgb(171, 71, 188);
                case GitStatusKind.Unknown:
                    return Color.FromArgb(244, 67, 54);
                default:
                    return Color.FromArgb(97, 97, 97);
            }
        }

        private static string GetStatusGlyph(GitStatusKind status) {
            switch(status) {
                case GitStatusKind.Clean:
                    return "✓";
                case GitStatusKind.Modified:
                    return "!";
                case GitStatusKind.Untracked:
                    return "?";
                case GitStatusKind.Mixed:
                    return "±";
                case GitStatusKind.Unknown:
                    return "!";
                default:
                    return null;
            }
        }

        private static string RunGit(string workdir, string args) {
            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = workdir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using(Process process = Process.Start(psi)) {
                if(process == null) {
                    return string.Empty;
                }
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(2000);
                return output ?? string.Empty;
            }
        }

        private static string FindGitRoot(string path) {
            try {
                DirectoryInfo dir = new DirectoryInfo(path);
                if(!dir.Exists) {
                    return null;
                }
                while(dir != null) {
                    if(Directory.Exists(Path.Combine(dir.FullName, ".git"))) {
                        return dir.FullName;
                    }
                    dir = dir.Parent;
                }
            }
            catch { }
            return null;
        }
    }
}
