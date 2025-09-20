using System;
using System.Collections.Generic;
using System.IO;

namespace QTTabBarLib {
    internal enum ComparisonDiffType {
        Identical,
        LeftOnly,
        RightOnly,
        FileMismatch,
        FolderExtraLeft,
        FolderExtraRight,
        FolderExtraBoth,
        FolderContentMismatch
    }

    internal enum ComparisonEntryKind {
        File,
        Directory
    }

    internal sealed class ComparisonRow {
        private readonly int depth;
        private readonly string relativePath;
        private readonly FileSystemInfo left;
        private readonly FileSystemInfo right;
        private readonly ComparisonEntryKind kind;

        internal ComparisonRow(int depth, string relativePath, FileSystemInfo left, FileSystemInfo right, ComparisonEntryKind kind, ComparisonDiffType category) {
            this.depth = depth;
            this.relativePath = relativePath ?? string.Empty;
            this.left = left;
            this.right = right;
            this.kind = kind;
            Category = category;
        }

        internal int Depth { get { return depth; } }
        internal string RelativePath { get { return relativePath; } }
        internal FileSystemInfo Left { get { return left; } }
        internal FileSystemInfo Right { get { return right; } }
        internal ComparisonEntryKind Kind { get { return kind; } }
        internal ComparisonDiffType Category { get; set; }

        internal bool IsDirectory {
            get { return kind == ComparisonEntryKind.Directory; }
        }
    }

    internal sealed class ComparisonResult {
        private readonly string leftRoot;
        private readonly string rightRoot;
        private readonly List<ComparisonRow> rows;
        private readonly List<string> addedRelativeFiles;
        private readonly List<string> addedRelativeDirectories;
        private readonly List<string> removedRelativeFiles;
        private readonly List<string> removedRelativeDirectories;
        private readonly List<string> changedRelativeFiles;

        internal ComparisonResult(string leftRoot, string rightRoot) {
            this.leftRoot = leftRoot;
            this.rightRoot = rightRoot;
            rows = new List<ComparisonRow>();
            addedRelativeFiles = new List<string>();
            addedRelativeDirectories = new List<string>();
            removedRelativeFiles = new List<string>();
            removedRelativeDirectories = new List<string>();
            changedRelativeFiles = new List<string>();
        }

        internal string LeftRoot { get { return leftRoot; } }
        internal string RightRoot { get { return rightRoot; } }
        internal List<ComparisonRow> Rows { get { return rows; } }
        internal List<string> AddedRelativeFiles { get { return addedRelativeFiles; } }
        internal List<string> AddedRelativeDirectories { get { return addedRelativeDirectories; } }
        internal List<string> RemovedRelativeFiles { get { return removedRelativeFiles; } }
        internal List<string> RemovedRelativeDirectories { get { return removedRelativeDirectories; } }
        internal List<string> ChangedRelativeFiles { get { return changedRelativeFiles; } }

        internal void Register(ComparisonRow row) {
            rows.Add(row);
            switch (row.Category) {
                case ComparisonDiffType.LeftOnly:
                    if (row.IsDirectory) removedRelativeDirectories.Add(row.RelativePath);
                    else removedRelativeFiles.Add(row.RelativePath);
                    break;
                case ComparisonDiffType.RightOnly:
                    if (row.IsDirectory) addedRelativeDirectories.Add(row.RelativePath);
                    else addedRelativeFiles.Add(row.RelativePath);
                    break;
                case ComparisonDiffType.FileMismatch:
                    changedRelativeFiles.Add(row.RelativePath);
                    break;
            }
        }
    }

    internal static class ComparisonEngine {
        private sealed class FolderSummary {
            internal List<ComparisonRow> Rows = new List<ComparisonRow>();
            internal bool HasDifferences;
            internal bool HasContentMismatch;
            internal bool LeftHasExtras;
            internal bool RightHasExtras;
        }

        internal static ComparisonResult Compare(string leftPath, string rightPath) {
            if (string.IsNullOrEmpty(leftPath)) throw new ArgumentNullException("leftPath");
            if (string.IsNullOrEmpty(rightPath)) throw new ArgumentNullException("rightPath");

            DirectoryInfo leftDir = null;
            DirectoryInfo rightDir = null;
            try {
                if (Directory.Exists(leftPath)) leftDir = new DirectoryInfo(leftPath);
            } catch { }
            try {
                if (Directory.Exists(rightPath)) rightDir = new DirectoryInfo(rightPath);
            } catch { }

            ComparisonResult result = new ComparisonResult(leftPath, rightPath);
            ComparisonRow rootRow = new ComparisonRow(0, string.Empty, leftDir, rightDir, ComparisonEntryKind.Directory, ComparisonDiffType.Identical);

            if (leftDir == null || rightDir == null) {
                if (leftDir == null && rightDir == null) {
                    result.Register(rootRow);
                    return result;
                }
                rootRow.Category = leftDir == null ? ComparisonDiffType.RightOnly : ComparisonDiffType.LeftOnly;
                result.Register(rootRow);
                return result;
            }

            FolderSummary summary = CompareDirectory(leftDir, rightDir, 1, string.Empty);
            rootRow.Category = DetermineFolderCategory(summary);
            result.Register(rootRow);
            foreach (ComparisonRow row in summary.Rows) {
                result.Register(row);
            }
            return result;
        }

        private static FolderSummary CompareDirectory(DirectoryInfo left, DirectoryInfo right, int depth, string relativePath) {
            FolderSummary summary = new FolderSummary();
            SortedDictionary<string, object> names = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, FileSystemInfo> leftEntries = new Dictionary<string, FileSystemInfo>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, FileSystemInfo> rightEntries = new Dictionary<string, FileSystemInfo>(StringComparer.OrdinalIgnoreCase);

            if (left != null && left.Exists) {
                try {
                    foreach (FileSystemInfo entry in SafeEnumerate(left)) {
                        if (!names.ContainsKey(entry.Name)) {
                            names.Add(entry.Name, null);
                        }
                        leftEntries[entry.Name] = entry;
                    }
                } catch { }
            }
            if (right != null && right.Exists) {
                try {
                    foreach (FileSystemInfo entry in SafeEnumerate(right)) {
                        if (!names.ContainsKey(entry.Name)) {
                            names.Add(entry.Name, null);
                        }
                        rightEntries[entry.Name] = entry;
                    }
                } catch { }
            }

            foreach (KeyValuePair<string, object> nameEntry in names) {
                string name = nameEntry.Key;
                FileSystemInfo leftEntry;
                leftEntries.TryGetValue(name, out leftEntry);
                FileSystemInfo rightEntry;
                rightEntries.TryGetValue(name, out rightEntry);
                string childRelative = CombineRelative(relativePath, name);
                ComparisonEntryKind kind = ResolveKind(leftEntry, rightEntry);
                ComparisonRow row = new ComparisonRow(depth, childRelative, leftEntry, rightEntry, kind, ComparisonDiffType.Identical);

                if (leftEntry == null) {
                    row.Category = ComparisonDiffType.RightOnly;
                    summary.HasDifferences = true;
                    summary.RightHasExtras = true;
                    summary.Rows.Add(row);
                    DirectoryInfo rightDir = rightEntry as DirectoryInfo;
                    if (rightDir != null) {
                        AppendSingleSide(rightDir, depth + 1, childRelative, summary, false);
                    }
                    continue;
                }
                if (rightEntry == null) {
                    row.Category = ComparisonDiffType.LeftOnly;
                    summary.HasDifferences = true;
                    summary.LeftHasExtras = true;
                    summary.Rows.Add(row);
                    DirectoryInfo leftDirOnly = leftEntry as DirectoryInfo;
                    if (leftDirOnly != null) {
                        AppendSingleSide(leftDirOnly, depth + 1, childRelative, summary, true);
                    }
                    continue;
                }

                if (row.IsDirectory) {
                    FolderSummary childSummary = CompareDirectory(leftEntry as DirectoryInfo, rightEntry as DirectoryInfo, depth + 1, childRelative);
                    row.Category = DetermineFolderCategory(childSummary);

                    if (childSummary.HasDifferences) summary.HasDifferences = true;
                    if (childSummary.HasContentMismatch) summary.HasContentMismatch = true;
                    if (childSummary.LeftHasExtras) summary.LeftHasExtras = true;
                    if (childSummary.RightHasExtras) summary.RightHasExtras = true;

                    if (row.Category == ComparisonDiffType.FolderContentMismatch) summary.HasContentMismatch = true;
                    else if (row.Category == ComparisonDiffType.FolderExtraLeft) summary.LeftHasExtras = true;
                    else if (row.Category == ComparisonDiffType.FolderExtraRight) summary.RightHasExtras = true;
                    else if (row.Category == ComparisonDiffType.FolderExtraBoth) {
                        summary.LeftHasExtras = true;
                        summary.RightHasExtras = true;
                    }

                    summary.Rows.Add(row);
                    summary.Rows.AddRange(childSummary.Rows);
                    continue;
                }

                bool same = FilesEqual(leftEntry as FileInfo, rightEntry as FileInfo);
                if (!same) {
                    row.Category = ComparisonDiffType.FileMismatch;
                    summary.HasDifferences = true;
                    summary.HasContentMismatch = true;
                }
                summary.Rows.Add(row);
            }

            return summary;
        }

        private static void AppendSingleSide(DirectoryInfo dir, int depth, string relativePath, FolderSummary summary, bool isLeftSide) {
            if (dir == null) return;
            foreach (FileSystemInfo entry in SafeEnumerate(dir)) {
                string childRelative = CombineRelative(relativePath, entry.Name);
                ComparisonEntryKind kind = entry is DirectoryInfo ? ComparisonEntryKind.Directory : ComparisonEntryKind.File;
                ComparisonRow row = new ComparisonRow(depth, childRelative, isLeftSide ? entry : null, isLeftSide ? null : entry, kind, isLeftSide ? ComparisonDiffType.LeftOnly : ComparisonDiffType.RightOnly);
                summary.Rows.Add(row);
                DirectoryInfo childDir = entry as DirectoryInfo;
                if (childDir != null) {
                    AppendSingleSide(childDir, depth + 1, childRelative, summary, isLeftSide);
                }
            }
        }

        private static ComparisonDiffType DetermineFolderCategory(FolderSummary summary) {
            if (summary == null || !summary.HasDifferences) return ComparisonDiffType.Identical;
            if (summary.HasContentMismatch) return ComparisonDiffType.FolderContentMismatch;
            if (summary.LeftHasExtras && summary.RightHasExtras) return ComparisonDiffType.FolderExtraBoth;
            if (summary.LeftHasExtras) return ComparisonDiffType.FolderExtraLeft;
            if (summary.RightHasExtras) return ComparisonDiffType.FolderExtraRight;
            return ComparisonDiffType.FolderContentMismatch;
        }

        private static IEnumerable<FileSystemInfo> SafeEnumerate(DirectoryInfo dir) {
            try {
                return dir.GetFileSystemInfos();
            } catch {
                return new FileSystemInfo[0];
            }
        }

        private static ComparisonEntryKind ResolveKind(FileSystemInfo left, FileSystemInfo right) {
            if (left is DirectoryInfo || right is DirectoryInfo) {
                return ComparisonEntryKind.Directory;
            }
            return ComparisonEntryKind.File;
        }

        private static string CombineRelative(string baseRelative, string name) {
            if (string.IsNullOrEmpty(baseRelative)) return name;
            return baseRelative + Path.DirectorySeparatorChar + name;
        }

        private static bool FilesEqual(FileInfo left, FileInfo right) {
            if (left == null || right == null) return false;
            try {
                if (!left.Exists || !right.Exists) return false;
                if (left.Length != right.Length) return false;
                if (left.Length == 0) return true;
                const int bufferSize = 64 * 1024;
                byte[] leftBuffer = new byte[bufferSize];
                byte[] rightBuffer = new byte[bufferSize];
                using (FileStream leftStream = left.OpenRead())
                using (FileStream rightStream = right.OpenRead()) {
                    while (true) {
                        int leftRead = leftStream.Read(leftBuffer, 0, bufferSize);
                        int rightRead = rightStream.Read(rightBuffer, 0, bufferSize);
                        if (leftRead != rightRead) return false;
                        if (leftRead == 0) break;
                        for (int i = 0; i < leftRead; i++) {
                            if (leftBuffer[i] != rightBuffer[i]) return false;
                        }
                    }
                }
                return true;
            } catch {
                return false;
            }
        }
    }
}
