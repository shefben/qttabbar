using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using QTTabBarLib.ExplorerBrowser;
using QTTabBarLib.ExplorerBrowser.WindowsForms;
using QTTabBarLib.Common;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class CompareSplitHost : UserControl {
        private readonly SplitContainer split;
        private readonly QTTabBarLib.ExplorerBrowser.WindowsForms.ExplorerBrowser leftBrowser;
        private readonly QTTabBarLib.ExplorerBrowser.WindowsForms.ExplorerBrowser rightBrowser;
        private readonly Label leftStatus;
        private readonly Label rightStatus;
        private string rootLeft;
        private string rootRight;
        private string currentLeftPath;
        private string currentRightPath;
        private bool suppressNavigation;
        private bool pendingInitialNavigation;

        public CompareSplitHost() {
            Dock = DockStyle.Fill;
            split = new SplitContainer {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 200,
                Panel2MinSize = 200,
                SplitterWidth = 6
            };

            leftBrowser = CreateBrowser();
            rightBrowser = CreateBrowser();
            split.Panel1.Controls.Add(leftBrowser);
            split.Panel2.Controls.Add(rightBrowser);

            leftStatus = CreateStatusLabel();
            rightStatus = CreateStatusLabel();
            split.Panel1.Controls.Add(leftStatus);
            split.Panel2.Controls.Add(rightStatus);
            leftBrowser.NavigationComplete += LeftBrowser_NavigationComplete;
            rightBrowser.NavigationComplete += RightBrowser_NavigationComplete;

            Controls.Add(split);
        }

        public event EventHandler<ComparisonResultEventArgs> ComparisonUpdated;

        public ComparisonResult CurrentResult { get { return currentResult; } }
        private ComparisonResult currentResult;

        public void Initialize(string leftPath, string rightPath) {
            rootLeft = NormalizePath(leftPath);
            rootRight = NormalizePath(rightPath);
            currentLeftPath = rootLeft;
            currentRightPath = rootRight;
            pendingInitialNavigation = true;
            TryExecuteInitialNavigation();
        }

        public string CurrentLeftPath
        {
            get { return currentLeftPath; }
        }
        public string CurrentRightPath
        {
            get { return currentRightPath; }
        }

        private static QTTabBarLib.ExplorerBrowser.WindowsForms.ExplorerBrowser CreateBrowser() {
            var browser = new QTTabBarLib.ExplorerBrowser.WindowsForms.ExplorerBrowser {
                Dock = DockStyle.Fill
            };
            ConfigureBrowser(browser);
            return browser;
        }

        private static void ConfigureBrowser(QTTabBarLib.ExplorerBrowser.WindowsForms.ExplorerBrowser browser) {
            try {
                browser.NavigationOptions.PaneVisibility.Navigation = QTTabBarLib.ExplorerBrowser.PaneVisibilityState.DoNotCare;
            } catch { }
            try {
                browser.ContentOptions.ViewMode = QTTabBarLib.ExplorerBrowser.ExplorerBrowserViewMode.Details;
                browser.ContentOptions.FullRowSelect = true;
                browser.ContentOptions.AlignLeft = true;
                browser.ContentOptions.AutoArrange = true;
            } catch { }
        }

        private static void ApplyDefaultView(QTTabBarLib.ExplorerBrowser.WindowsForms.ExplorerBrowser browser) {
            try {
                QTTabBarLib.Common.IFolderView2 view2 = browser.GetFolderView2();
                if (view2 != null) {
                    try {
                        SortColumn sortColumn = new SortColumn(SystemProperties.System.ItemNameDisplay, SortDirection.Ascending);
                        int size = Marshal.SizeOf(typeof(SortColumn));
                        IntPtr buffer = Marshal.AllocHGlobal(size);
                        try {
                            Marshal.StructureToPtr(sortColumn, buffer, false);
                            view2.SetSortColumns(buffer, 1);
                        }
                        finally {
                            Marshal.FreeHGlobal(buffer);
                        }
                    } finally {
                        Marshal.ReleaseComObject(view2);
                    }
                }
            } catch { }
        }

        private static Label CreateStatusLabel() {
            Label label = new Label();
            label.Dock = DockStyle.Bottom;
            label.Height = 20;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = SystemColors.GrayText;
            label.Padding = new Padding(4, 0, 4, 0);
            return label;
        }

        private void LeftBrowser_NavigationComplete(object sender, QTTabBarLib.ExplorerBrowser.NavigationCompleteEventArgs e) {
            string path = GetPath(e != null ? e.NewLocation : null);
            if (!string.IsNullOrEmpty(path)) currentLeftPath = path;
            ApplyDefaultView(leftBrowser);
            UpdateStatusLabels();
            if (!suppressNavigation) {
                TrySyncPartner(true);
            }
            UpdateComparison();
        }

        private void RightBrowser_NavigationComplete(object sender, QTTabBarLib.ExplorerBrowser.NavigationCompleteEventArgs e) {
            string path = GetPath(e != null ? e.NewLocation : null);
            if (!string.IsNullOrEmpty(path)) currentRightPath = path;
            ApplyDefaultView(rightBrowser);
            UpdateStatusLabels();
            if (!suppressNavigation) {
                TrySyncPartner(false);
            }
            UpdateComparison();
        }

        private void TryExecuteInitialNavigation() {
            if (!pendingInitialNavigation) {
                return;
            }
            if (!IsHandleCreated) {
                return;
            }
            pendingInitialNavigation = false;
            NavigatePair(rootLeft, rootRight, true);
        }

        private void TrySyncPartner(bool fromLeft) {
            if (string.IsNullOrEmpty(rootLeft) || string.IsNullOrEmpty(rootRight)) return;
            string sourceRoot = fromLeft ? rootLeft : rootRight;
            string targetRoot = fromLeft ? rootRight : rootLeft;
            string sourcePath = fromLeft ? currentLeftPath : currentRightPath;
            string relative = GetRelativePath(sourceRoot, sourcePath);
            if (relative == null) return;
            string targetPath = CombineSafe(targetRoot, relative);
            if (!Directory.Exists(targetPath)) return;
            string currentTarget = fromLeft ? currentRightPath : currentLeftPath;
            if (PathsEqual(currentTarget, targetPath)) return;
            suppressNavigation = true;
            try {
                if (fromLeft) {
                    rightBrowser.Navigate(targetPath);
                } else {
                    leftBrowser.Navigate(targetPath);
                }
            } catch { }
            finally {
                suppressNavigation = false;
            }
        }

        private void NavigatePair(string left, string right, bool force) {
            suppressNavigation = true;
            try {
                if (force || !PathsEqual(currentLeftPath, left)) {
                    leftBrowser.Navigate(left);
                }
                if (force || !PathsEqual(currentRightPath, right)) {
                    rightBrowser.Navigate(right);
                }
            } catch { }
            finally {
                suppressNavigation = false;
            }
            UpdateStatusLabels();
            UpdateComparison();
        }

        private void UpdateComparison() {
            var left = currentLeftPath;
            var right = currentRightPath;
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right)) {
                CompareOverlayManager.ClearDetailedContext();
                return;
            }
            ComparisonResult result;
            try {
                result = ComparisonEngine.Compare(left, right);
            } catch {
                CompareOverlayManager.ClearDetailedContext();
                return;
            }
            currentResult = result;
            ApplyOverlay(result);
            EventHandler<ComparisonResultEventArgs> handler = ComparisonUpdated;
            if (handler != null) {
                handler(this, new ComparisonResultEventArgs(result));
            }
        }

        private void ApplyOverlay(ComparisonResult result) {
            var leftMap = new Dictionary<string, Dictionary<string, DiffVisualStyle>>(StringComparer.OrdinalIgnoreCase);
            var rightMap = new Dictionary<string, Dictionary<string, DiffVisualStyle>>(StringComparer.OrdinalIgnoreCase);
            var leftOnlyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rightOnlyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var changedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ComparisonRow row in result.Rows) {
                if (row.Depth == 0) continue;
                string relative = row.RelativePath;
                string parentRelative = Path.GetDirectoryName(relative) ?? string.Empty;
                string name = GetItemName(row);
                if (string.IsNullOrEmpty(name)) continue;

                string leftFolder = CombineSafe(result.LeftRoot, parentRelative);
                string rightFolder = CombineSafe(result.RightRoot, parentRelative);

                DiffVisualStyle? leftStyle = GetLeftStyle(row.Category);
                DiffVisualStyle? rightStyle = GetRightStyle(row.Category);

                if (leftStyle.HasValue) AddStyle(leftMap, leftFolder, name, leftStyle.Value);
                if (rightStyle.HasValue) AddStyle(rightMap, rightFolder, name, rightStyle.Value);

                if (string.IsNullOrEmpty(parentRelative)) {
                    switch (row.Category) {
                        case ComparisonDiffType.LeftOnly:
                            leftOnlyNames.Add(name);
                            break;
                        case ComparisonDiffType.RightOnly:
                            rightOnlyNames.Add(name);
                            break;
                        case ComparisonDiffType.FileMismatch:
                            changedNames.Add(name);
                            break;
                    }
                }
            }

            CompareOverlayManager.SetDetailedContext(result.LeftRoot, result.RightRoot, leftMap, rightMap);
            CompareOverlayManager.SetContext(result.LeftRoot, result.RightRoot, rightOnlyNames, leftOnlyNames, changedNames);
        }

        private static string GetItemName(ComparisonRow row) {
            if (row.Left != null) return row.Left.Name;
            if (row.Right != null) return row.Right.Name;
            return Path.GetFileName(row.RelativePath);
        }

        private static void AddStyle(Dictionary<string, Dictionary<string, DiffVisualStyle>> map, string folder, string name, DiffVisualStyle style) {
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(name)) return;
            Dictionary<string, DiffVisualStyle> inner;
            if (!map.TryGetValue(folder, out inner)) {
                inner = new Dictionary<string, DiffVisualStyle>(StringComparer.OrdinalIgnoreCase);
                map[folder] = inner;
            }
            inner[name] = style;
        }

        private static DiffVisualStyle? GetLeftStyle(ComparisonDiffType category) {
            switch (category) {
                case ComparisonDiffType.LeftOnly:
                    return DiffVisualPalette.FileMissing;
                case ComparisonDiffType.FileMismatch:
                    return DiffVisualPalette.FileMismatch;
                case ComparisonDiffType.FolderExtraLeft:
                    return DiffVisualPalette.FolderExtraPrimary;
                case ComparisonDiffType.FolderExtraRight:
                    return DiffVisualPalette.FolderExtraSecondary;
                case ComparisonDiffType.FolderExtraBoth:
                    return DiffVisualPalette.FolderExtraPrimary;
                case ComparisonDiffType.FolderContentMismatch:
                    return DiffVisualPalette.FolderMismatch;
                default:
                    return null;
            }
        }

        private static DiffVisualStyle? GetRightStyle(ComparisonDiffType category) {
            switch (category) {
                case ComparisonDiffType.RightOnly:
                    return DiffVisualPalette.FileMissing;
                case ComparisonDiffType.FileMismatch:
                    return DiffVisualPalette.FileMismatch;
                case ComparisonDiffType.FolderExtraLeft:
                    return DiffVisualPalette.FolderExtraSecondary;
                case ComparisonDiffType.FolderExtraRight:
                    return DiffVisualPalette.FolderExtraPrimary;
                case ComparisonDiffType.FolderExtraBoth:
                    return DiffVisualPalette.FolderExtraPrimary;
                case ComparisonDiffType.FolderContentMismatch:
                    return DiffVisualPalette.FolderMismatch;
                default:
                    return null;
            }
        }

        private void UpdateStatusLabels() {
            leftStatus.Text = currentLeftPath;
            rightStatus.Text = currentRightPath;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            TryExecuteInitialNavigation();
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                CompareOverlayManager.ClearDetailedContext();
                leftBrowser.NavigationComplete -= LeftBrowser_NavigationComplete;
                rightBrowser.NavigationComplete -= RightBrowser_NavigationComplete;
            }
            base.Dispose(disposing);
        }

        private static string GetPath(ShellObject obj) {
            try { return NormalizePath(obj == null ? null : obj.ParsingName); } catch { return null; }
        }

        private static string NormalizePath(string path) {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            try { return Path.GetFullPath(path); } catch { return path; }
        }

        private static bool PathsEqual(string a, string b) {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return true;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return string.Equals(NormalizePath(a), NormalizePath(b), StringComparison.OrdinalIgnoreCase);
        }

        private static string CombineSafe(string root, string relative) {
            if (string.IsNullOrEmpty(relative)) return root;
            if (string.IsNullOrEmpty(root)) return relative;
            return NormalizePath(Path.Combine(root, relative));
        }

        private static string GetRelativePath(string root, string fullPath) {
            if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(fullPath)) return null;
            try {
                Uri rootUri = new Uri(AppendDirectorySeparator(root));
                Uri pathUri = new Uri(AppendDirectorySeparator(fullPath));
                if (!rootUri.IsBaseOf(pathUri)) return null;
                string rel = Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString());
                return rel.Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
            } catch {
                if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)) {
                    string rel = fullPath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    return rel;
                }
                return null;
            }
        }

        private static string AppendDirectorySeparator(string path) {
            if (string.IsNullOrEmpty(path)) return path;
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()) && !path.EndsWith(Path.AltDirectorySeparatorChar.ToString())) {
                return path + Path.DirectorySeparatorChar;
            }
            return path;
        }
    }

    internal sealed class ComparisonResultEventArgs : EventArgs {
        private readonly ComparisonResult result;
        internal ComparisonResultEventArgs(ComparisonResult result) {
            this.result = result;
        }
        public ComparisonResult Result { get { return result; } }
    }

    internal static class DiffVisualPalette {
        internal static readonly DiffVisualStyle FileMissing = new DiffVisualStyle(Color.FromArgb(220, 40, 40), Color.MediumPurple);
        internal static readonly DiffVisualStyle FileMismatch = new DiffVisualStyle(Color.FromArgb(220, 60, 0), Color.FromArgb(255, 240, 0));
        internal static readonly DiffVisualStyle FolderExtraPrimary = new DiffVisualStyle(Color.FromArgb(110, 50, 140), Color.FromArgb(255, 240, 0));
        internal static readonly DiffVisualStyle FolderExtraSecondary = new DiffVisualStyle(Color.FromArgb(110, 50, 140), Color.White);
        internal static readonly DiffVisualStyle FolderMismatch = new DiffVisualStyle(Color.FromArgb(200, 30, 30), Color.FromArgb(0, 200, 0));
    }

    internal struct DiffVisualStyle {
        internal DiffVisualStyle(Color background, Color foreground) {
            Background = background;
            Foreground = foreground;
        }
        internal Color Background;
        internal Color Foreground;
    }
}
