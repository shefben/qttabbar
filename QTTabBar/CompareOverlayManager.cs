using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;

namespace QTTabBarLib {
    internal enum DiffMark { None, Added, Removed, Changed }

    internal static class CompareOverlayManager {
        private static string leftPath;
        private static string rightPath;
        private static HashSet<string> added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static HashSet<string> removed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static HashSet<string> changed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, Dictionary<string, DiffVisualStyle>> detailedLeft = new Dictionary<string, Dictionary<string, DiffVisualStyle>>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, Dictionary<string, DiffVisualStyle>> detailedRight = new Dictionary<string, Dictionary<string, DiffVisualStyle>>(StringComparer.OrdinalIgnoreCase);

        public static void SetContext(string left, string right, IEnumerable<string> add, IEnumerable<string> rem, IEnumerable<string> chg) {
            leftPath = left; rightPath = right;
            added = new HashSet<string>(add ?? new string[0], StringComparer.OrdinalIgnoreCase);
            removed = new HashSet<string>(rem ?? new string[0], StringComparer.OrdinalIgnoreCase);
            changed = new HashSet<string>(chg ?? new string[0], StringComparer.OrdinalIgnoreCase);
        }
        public static void Clear() { leftPath = rightPath = null; added.Clear(); removed.Clear(); changed.Clear(); ClearDetailedContext(); }
        public static bool IsActive { get { return !string.IsNullOrEmpty(leftPath) && !string.IsNullOrEmpty(rightPath); } }
        public static string LeftPath { get { return leftPath; } }
        public static string RightPath { get { return rightPath; } }
        public static IEnumerable<string> Added { get { return added; } }
        public static IEnumerable<string> Removed { get { return removed; } }
        public static IEnumerable<string> Changed { get { return changed; } }

        public static void SetDetailedContext(string leftRoot, string rightRoot, Dictionary<string, Dictionary<string, DiffVisualStyle>> left, Dictionary<string, Dictionary<string, DiffVisualStyle>> right) {
            detailedLeft = CloneDetailedMap(left);
            detailedRight = CloneDetailedMap(right);
        }

        public static void ClearDetailedContext() {
            detailedLeft = new Dictionary<string, Dictionary<string, DiffVisualStyle>>(StringComparer.OrdinalIgnoreCase);
            detailedRight = new Dictionary<string, Dictionary<string, DiffVisualStyle>>(StringComparer.OrdinalIgnoreCase);
        }

        public static bool TryGetDetailedStyle(string folderPath, string itemName, out DiffVisualStyle style) {
            style = new DiffVisualStyle(Color.Empty, Color.Empty);
            if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(itemName)) return false;
            Dictionary<string, DiffVisualStyle> map;
            if (detailedLeft.TryGetValue(folderPath, out map) && map.TryGetValue(itemName, out style)) return true;
            if (detailedRight.TryGetValue(folderPath, out map) && map.TryGetValue(itemName, out style)) return true;
            return false;
        }

        private static Dictionary<string, Dictionary<string, DiffVisualStyle>> CloneDetailedMap(Dictionary<string, Dictionary<string, DiffVisualStyle>> source) {
            Dictionary<string, Dictionary<string, DiffVisualStyle>> result = new Dictionary<string, Dictionary<string, DiffVisualStyle>>(StringComparer.OrdinalIgnoreCase);
            if (source == null) return result;
            foreach (KeyValuePair<string, Dictionary<string, DiffVisualStyle>> folder in source) {
                Dictionary<string, DiffVisualStyle> inner = new Dictionary<string, DiffVisualStyle>(StringComparer.OrdinalIgnoreCase);
                if (folder.Value != null) {
                    foreach (KeyValuePair<string, DiffVisualStyle> item in folder.Value) {
                        inner[item.Key] = item.Value;
                    }
                }
                result[folder.Key] = inner;
            }
            return result;
        }

        public static DiffMark GetMark(string currentFolder, string itemName) {
            if (string.IsNullOrEmpty(currentFolder) || string.IsNullOrEmpty(itemName)) return DiffMark.None;
            if (currentFolder.PathEquals(leftPath)) {
                if (added.Contains(itemName)) return DiffMark.Added;
                if (removed.Contains(itemName)) return DiffMark.Removed; // show deleted relative to left
                if (changed.Contains(itemName)) return DiffMark.Changed;
            }
            return DiffMark.None;
        }
    }
}
