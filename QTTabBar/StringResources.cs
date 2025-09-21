//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2022  Quizo, Paul Accisano, indiff
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Globalization;

namespace QTTabBarLib {
    internal static class StringResources {
        private static readonly object SyncRoot = new object();
        private static bool initialized;

        public static void Initialize() {
            EnsureInitialized();
        }

        public static string Pluralize(string pattern, int count) {
            if(string.IsNullOrEmpty(pattern)) {
                return string.Empty;
            }

            string[] forms = pattern.Split(new[] { ';' }, StringSplitOptions.None);
            string format;
            if(forms.Length <= 1) {
                format = forms[0];
            }
            else {
                format = count == 1 ? forms[0] : forms[Math.Min(forms.Length - 1, 1)];
            }

            try {
                return string.Format(CultureInfo.CurrentCulture, format, count);
            }
            catch(FormatException) {
                return format;
            }
        }

        public static class Dialogs {
            private static readonly string[] Fallback = {
                "Another item with the same name already exists.",
                "Please choose a different name.",
                "Rename"
            };

            public static string[] _Dialog {
                get { return GetStrings("Dialogs", Fallback); }
            }
        }

        public static class Global {
            private static readonly string[] NotifyIconFallback = {
                "Explorer",
                "{0} windows",
                "Restore all",
                "Close all"
            };

            private static readonly string[] ViewSyncFallback = {
                "Off",
                "Navigation",
                "Scroll",
                "Navigation synchronization canceled."
            };

            public static string[] NotifyIcon {
                get { return GetStrings("TrayIcon", NotifyIconFallback); }
            }

            public static string[] ViewSync {
                get { return GetStrings("ViewSync", ViewSyncFallback); }
            }
        }

        private static void EnsureInitialized() {
            if(initialized) {
                return;
            }

            lock(SyncRoot) {
                if(initialized) {
                    return;
                }

                try {
                    QTUtility.ValidateTextResources();
                }
                catch(Exception ex) {
                    QTUtility2.MakeErrorLog(ex, "StringResources.Initialize");
                }

                initialized = true;
            }
        }

        private static string[] GetStrings(string key, string[] fallback) {
            EnsureInitialized();

            var dictionary = QTUtility.TextResourcesDic;
            if(dictionary == null) {
                return CloneArray(fallback);
            }

            string[] values;
            if(!dictionary.TryGetValue(key, out values) || values == null || values.Length == 0) {
                return CloneArray(fallback);
            }

            if(fallback != null && values.Length < fallback.Length) {
                string[] extended = new string[fallback.Length];
                Array.Copy(values, extended, values.Length);
                Array.Copy(fallback, values.Length, extended, values.Length, fallback.Length - values.Length);
                return extended;
            }

            return CloneArray(values);
        }

        private static string[] CloneArray(string[] source) {
            if(source == null || source.Length == 0) {
                return new string[0];
            }
            return (string[])source.Clone();
        }
    }
}
