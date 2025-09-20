using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace QTTabBarLib {
    internal static class ShellZip {
        private static Type ShellType = Type.GetTypeFromProgID("Shell.Application");

        public static bool IsSupported(string archivePath) {
            try { return GetFolder(archivePath) != null; } catch { return false; }
        }

        public static List<string> ListEntries(string archivePath) {
            var list = new List<string>();
            try {
                object folder = GetFolder(archivePath);
                if (folder == null) return list;
                object items = folder.GetType().InvokeMember("Items", BindingFlags.InvokeMethod, null, folder, null);
                var enumerable = items as IEnumerable;
                if (enumerable == null) return list;
                foreach (object item in enumerable) {
                    string name = Convert.ToString(item.GetType().InvokeMember("Path", BindingFlags.GetProperty, null, item, null));
                    if (!string.IsNullOrEmpty(name)) {
                        // Normalize to relative-like names for display; store the Name property
                        string rel = Convert.ToString(item.GetType().InvokeMember("Name", BindingFlags.GetProperty, null, item, null));
                        string full = Convert.ToString(item.GetType().InvokeMember("Path", BindingFlags.GetProperty, null, item, null));
                        // Use the folder item Name (may not include parents); best effort
                        list.Add(rel);
                    }
                }
            } catch { }
            return list;
        }

        public static bool Extract(string archivePath, string destFolder, IEnumerable<string> entryNames) {
            try {
                object shell = Activator.CreateInstance(ShellType);
                object src = ShellType.InvokeMember("NameSpace", BindingFlags.InvokeMethod, null, shell, new object[] { archivePath });
                if (src == null) return false;
                object dst = ShellType.InvokeMember("NameSpace", BindingFlags.InvokeMethod, null, shell, new object[] { destFolder });
                if (dst == null) return false;
                object items = src.GetType().InvokeMember("Items", BindingFlags.InvokeMethod, null, src, null);
                var enumerable = items as IEnumerable; if (enumerable == null) return false;
                var set = new HashSet<string>(entryNames ?? new string[0], StringComparer.OrdinalIgnoreCase);
                foreach (object item in enumerable) {
                    string name = Convert.ToString(item.GetType().InvokeMember("Name", BindingFlags.GetProperty, null, item, null));
                    if (set.Count == 0 || set.Contains(name)) {
                        // 16 = FOF_NOCONFIRMMKDIR; 4 = FOF_SILENT
                        dst.GetType().InvokeMember("CopyHere", BindingFlags.InvokeMethod, null, dst, new object[] { item, 16 | 4 });
                    }
                }
                return true;
            } catch { return false; }
        }

        private static object GetFolder(string path) {
            try {
                object shell = Activator.CreateInstance(ShellType);
                return ShellType.InvokeMember("NameSpace", BindingFlags.InvokeMethod, null, shell, new object[] { path });
            } catch { return null; }
        }
    }
}

