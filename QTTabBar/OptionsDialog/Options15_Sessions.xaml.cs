using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QTTabBarLib {
    internal partial class Options15_Sessions : OptionsDialogTab {
        private readonly ObservableCollection<SessionEntry> sessions = new ObservableCollection<SessionEntry>();

        internal Options15_Sessions() {
            InitializeComponent();
            lstSessions.ItemsSource = sessions;
            lstSessions.KeyDown += lstSessions_KeyDown;
            UpdateUiState();
        }

        public override void InitializeConfig() {
            RefreshSessions();
        }

        public override void ResetConfig() {
            RefreshSessions();
        }

        public override void CommitConfig() {
        }

        private void RefreshSessions() {
            sessions.Clear();
            foreach(var session in TabSessionManager.GetSavedSessions()) {
                var entry = SessionEntry.Create(session);
                if(entry != null) {
                    sessions.Add(entry);
                }
            }
            txtEmpty.Visibility = sessions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if(sessions.Count > 0) {
                lstSessions.SelectedIndex = 0;
            }
            UpdateUiState();
        }

        private void UpdateUiState() {
            btnOpen.IsEnabled = lstSessions.SelectedItem is SessionEntry;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e) {
            RefreshSessions();
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e) {
            LaunchSelectedSession();
        }

        private void lstSessions_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UpdateUiState();
        }

        private void lstSessions_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if(LaunchSelectedSession()) {
                e.Handled = true;
            }
        }

        private void lstSessions_KeyDown(object sender, KeyEventArgs e) {
            if(e.Key == Key.Enter && LaunchSelectedSession()) {
                e.Handled = true;
            }
        }

        private bool LaunchSelectedSession() {
            var entry = lstSessions.SelectedItem as SessionEntry;
            if(entry == null) return false;
            if(!TabSessionManager.LaunchSession(entry.Session)) {
                MessageBox.Show(
                    GetResource(5, "Unable to open the selected session."),
                    GetResource(0, "Saved Sessions"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private static string GetResource(int index, string fallback) {
            try {
                return QTUtility.TextResourcesDic["Options_Page15_Sessions"][index];
            } catch {
                return fallback;
            }
        }

        private sealed class SessionEntry {
            public TabSessionManager.SavedSession Session { get; private set; }
            public string Title { get; private set; }
            public string Summary { get; private set; }
            public string Tooltip { get; private set; }

            public static SessionEntry Create(TabSessionManager.SavedSession session) {
                if(session == null || session.Tabs == null || session.Tabs.Length == 0) return null;
                var visibleTabs = session.Tabs.Where(t => t != null && !string.IsNullOrEmpty(t.Path)).ToList();
                if(visibleTabs.Count == 0) return null;
                string timestamp = session.LastWriteTimeLocal.ToString("g");
                string header = string.Format(CultureInfo.CurrentCulture, "{0} - {1}", timestamp, FormatTabCount(visibleTabs.Count));
                string[] names = visibleTabs
                    .Select(t => string.IsNullOrEmpty(t.Name) ? SafeFileName(t.Path) : t.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Take(5)
                    .ToArray();
                string summary = names.Length == 0 ? string.Empty : string.Join(", ", names);
                string tooltip = string.Join(Environment.NewLine, visibleTabs.Select(t => t.Path ?? string.Empty).ToArray());
                return new SessionEntry {
                    Session = session,
                    Title = header,
                    Summary = summary,
                    Tooltip = tooltip
                };
            }

            private static string FormatTabCount(int count) {
                if(count == 1) return "1 tab";
                return string.Format(CultureInfo.CurrentCulture, "{0} tabs", count);
            }

            private static string SafeFileName(string path) {
                try {
                    if(string.IsNullOrEmpty(path)) return string.Empty;
                    string name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    return string.IsNullOrEmpty(name) ? path : name;
                } catch {
                    return path;
                }
            }
        }
    }
}