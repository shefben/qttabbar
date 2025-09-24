using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace QTTabBarLib {
    internal static class TagManager {
        private static readonly string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QTTabBar");
        private static readonly string TagsFilePath = Path.Combine(DirectoryPath, "tags.tsv");
        private static readonly string TagDefinitionsPath = Path.Combine(DirectoryPath, "tagdefs.tsv");

        private static readonly object Gate = new object();

        private static Dictionary<string, HashSet<string>> tagAssignments;
        private static Dictionary<string, TagDefinition> tagDefinitions;
        private static Dictionary<string, TagVisualState> visualCache;

        private static bool highlightTagged;
        private static bool dimUntagged;

        internal static event EventHandler<TagVisualChangedEventArgs> TagVisualChanged;

        public static bool HighlightTagged {
            get { return highlightTagged; }
            set {
                if(highlightTagged == value) {
                    return;
                }
                highlightTagged = value;
                try {
                    if(Config.Misc != null) {
                        Config.Misc.HighlightTagged = value;
                    }
                } catch { }
                BroadcastTagChanges(TagVisualChangedEventArgs.Global);
            }
        }

        public static bool DimUntagged {
            get { return dimUntagged; }
            set {
                if(dimUntagged == value) {
                    return;
                }
                dimUntagged = value;
                try {
                    if(Config.Misc != null) {
                        Config.Misc.DimUntagged = value;
                    }
                } catch { }
                BroadcastTagChanges(TagVisualChangedEventArgs.Global);
            }
        }

        private static bool IsNullOrWhiteSpaceCompat(string value) {
            return string.IsNullOrEmpty(value) || value.Trim().Length == 0;
        }

        private static void Ensure() {
            if(tagAssignments != null) {
                return;
            }
            lock(Gate) {
                if(tagAssignments != null) {
                    return;
                }
                tagAssignments = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                tagDefinitions = new Dictionary<string, TagDefinition>(StringComparer.OrdinalIgnoreCase);
                visualCache = new Dictionary<string, TagVisualState>(StringComparer.OrdinalIgnoreCase);
                try {
                    Directory.CreateDirectory(DirectoryPath);
                }
                catch { }
                LoadAssignments();
                LoadDefinitions();
                RebuildVisualCache();

                // Sync with config values
                try {
                    if(Config.Misc != null) {
                        highlightTagged = Config.Misc.HighlightTagged;
                        dimUntagged = Config.Misc.DimUntagged;
                    }
                    else {
                        // Use defaults if Config not ready
                        highlightTagged = true;
                        dimUntagged = false;
                    }
                } catch {
                    // Use defaults if Config access fails
                    highlightTagged = true;
                    dimUntagged = false;
                }
            }
        }

        private static void LoadAssignments() {
            try {
                if(!File.Exists(TagsFilePath)) {
                    return;
                }

                foreach(string line in File.ReadAllLines(TagsFilePath)) {
                    if(IsNullOrWhiteSpaceCompat(line)) {
                        continue;
                    }
                    string[] parts = SplitLegacyAware(line);
                    if(parts.Length < 2) {
                        continue;
                    }
                    string path = parts[0];
                    HashSet<string> tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach(string token in parts[1].Split(',')) {
                        string tag = (token ?? string.Empty).Trim();
                        if(tag.Length > 0) {
                            tags.Add(tag);
                        }
                    }
                    tagAssignments[path] = tags;
                }
            }
            catch { }
        }

        private static void LoadDefinitions() {
            try {
                if(!File.Exists(TagDefinitionsPath)) {
                    return;
                }
                int version = 1;
                foreach(string line in File.ReadAllLines(TagDefinitionsPath)) {
                    if(IsNullOrWhiteSpaceCompat(line)) {
                        continue;
                    }
                    if(line.StartsWith("#")) {
                        if(line.StartsWith("#version=", StringComparison.OrdinalIgnoreCase)) {
                            int.TryParse(line.Substring(9), NumberStyles.Integer, CultureInfo.InvariantCulture, out version);
                        }
                        continue;
                    }
                    string[] parts = line.Split('\t');
                    if(parts.Length < 2) {
                        parts = SplitLegacyAware(line);
                    }
                    if(parts.Length == 0) {
                        continue;
                    }
                    string name = (parts[0] ?? string.Empty).Trim();
                    if(name.Length == 0) {
                        continue;
                    }
                    TagDefinition definition = GetOrCreateDefinition(name);
                    if(parts.Length > 1 && !IsNullOrWhiteSpaceCompat(parts[1])) {
                        Color color;
                        if(TryParseColor(parts[1], out color)) {
                            definition.Color = color;
                        }
                    }
                    if(version >= 2 && parts.Length > 2) {
                        int priority;
                        if(int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out priority)) {
                            definition.Priority = priority;
                        }
                    }
                }
            }
            catch { }
        }

        public static void AddTags(IEnumerable<string> paths, IEnumerable<string> tags) {
            if(paths == null || tags == null) {
                return;
            }
            Ensure();
            HashSet<string> uniqueTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> tagList = new List<string>();
            foreach(string tag in tags) {
                string trimmed = (tag ?? string.Empty).Trim();
                if(trimmed.Length == 0) {
                    continue;
                }
                if(uniqueTags.Add(trimmed)) {
                    tagList.Add(trimmed);
                }
            }
            if(uniqueTags.Count == 0) {
                return;
            }

            bool anyAssignmentsChanged = false;
            HashSet<string> affectedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> affectedPaths = new List<string>();
            foreach(string rawPath in paths) {
                if(IsNullOrWhiteSpaceCompat(rawPath)) {
                    continue;
                }
                string path = rawPath.Trim();
                if(path.Length == 0) {
                    continue;
                }
                HashSet<string> current;
                if(!tagAssignments.TryGetValue(path, out current)) {
                    current = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    tagAssignments[path] = current;
                }
                bool pathChanged = false;
                foreach(string tag in tagList) {
                    if(current.Add(tag)) {
                        pathChanged = true;
                    }
                }
                if(pathChanged) {
                    anyAssignmentsChanged = true;
                    if(UpdateVisualCacheForPath(path, current) && affectedSet.Add(path)) {
                        affectedPaths.Add(path);
                    }
                }
            }

            if(anyAssignmentsChanged) {
                SaveAssignments();
                if(affectedPaths.Count > 0) {
                    BroadcastTagChanges(TagVisualChangedEventArgs.ForPaths(affectedPaths, tagList));
                }
            }
        }

        public static string GetTagSummary(string path) {
            Ensure();
            if(string.IsNullOrEmpty(path)) {
                return string.Empty;
            }
            HashSet<string> set;
            if(tagAssignments.TryGetValue(path, out set) && set.Count > 0) {
                return string.Join(",", set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray());
            }
            return string.Empty;
        }

        public static bool HasTags(string path) {
            Ensure();
            if(string.IsNullOrEmpty(path)) {
                return false;
            }
            return GetVisualState(path).HasTag;
        }

        public static Color? GetTagColorForPath(string path) {
            return GetVisualState(path).ForegroundColor;
        }

        public static bool TryGetTagColor(string tag, out Color color) {
            Ensure();
            color = Color.Empty;
            if(string.IsNullOrEmpty(tag)) {
                return false;
            }
            TagDefinition definition;
            if(tagDefinitions != null && tagDefinitions.TryGetValue(tag, out definition) && definition != null && definition.Color.HasValue) {
                color = definition.Color.Value;
                return true;
            }
            return false;
        }

        public static void SetTagColor(string tag, Color? color) {
            if(IsNullOrWhiteSpaceCompat(tag)) {
                return;
            }
            Ensure();
            tag = tag.Trim();
            bool changed = false;
            TagDefinition definition;
            if(!tagDefinitions.TryGetValue(tag, out definition)) {
                if(!color.HasValue) {
                    return;
                }
                definition = new TagDefinition(tag);
                tagDefinitions[tag] = definition;
            }

            Color? previous = definition.Color;
            if(color.HasValue) {
                if(!previous.HasValue || previous.Value != color.Value) {
                    definition.Color = color.Value;
                    changed = true;
                }
            }
            else {
                if(previous.HasValue) {
                    definition.Color = null;
                    changed = true;
                }
                if(!definition.HasSerializableData) {
                    tagDefinitions.Remove(tag);
                }
            }

            if(changed) {
                SaveDefinitions();
                HashSet<string> affectedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                List<string> affectedPaths = new List<string>();
                foreach(KeyValuePair<string, HashSet<string>> assignment in tagAssignments) {
                    if(assignment.Value != null && assignment.Value.Contains(tag)) {
                        if(UpdateVisualCacheForPath(assignment.Key, assignment.Value) && affectedSet.Add(assignment.Key)) {
                            affectedPaths.Add(assignment.Key);
                        }
                    }
                }
                if(affectedPaths.Count > 0) {
                    BroadcastTagChanges(TagVisualChangedEventArgs.ForPaths(affectedPaths, new[] { tag }));
                }
            }
        }

        private static void SaveAssignments() {
            try {
                StringBuilder builder = new StringBuilder();
                foreach(KeyValuePair<string, HashSet<string>> pair in tagAssignments) {
                    if(pair.Value == null || pair.Value.Count == 0) {
                        continue;
                    }
                    builder.Append(pair.Key).Append('\t').Append(string.Join(",", pair.Value.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToArray())).AppendLine();
                }
                File.WriteAllText(TagsFilePath, builder.ToString());
            }
            catch { }
        }

        private static void SaveDefinitions() {
            try {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("#version=2");
                foreach(KeyValuePair<string, TagDefinition> pair in tagDefinitions.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)) {
                    TagDefinition definition = pair.Value;
                    if(definition == null || !definition.HasSerializableData) {
                        continue;
                    }
                    builder.Append(pair.Key).Append('\t');
                    if(definition.Color.HasValue) {
                        builder.Append(ColorTranslator.ToHtml(definition.Color.Value));
                    }
                    builder.Append('\t').Append(definition.Priority);
                    builder.AppendLine();
                }
                File.WriteAllText(TagDefinitionsPath, builder.ToString());
            }
            catch { }
        }

        private static bool TryParseColor(string value, out Color color) {
            color = Color.Empty;
            if(IsNullOrWhiteSpaceCompat(value)) {
                return false;
            }
            value = value.Trim();
            try {
                color = ColorTranslator.FromHtml(value);
                return true;
            }
            catch {
                if(value.Length == 6) {
                    int rgb;
                    if(int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb)) {
                        color = Color.FromArgb(255, (rgb >> 16) & 0xff, (rgb >> 8) & 0xff, rgb & 0xff);
                        return true;
                    }
                }
            }
            return false;
        }

        internal static TagVisualState GetVisualState(string path) {
            Ensure();
            if(string.IsNullOrEmpty(path)) {
                return TagVisualState.Empty;
            }
            TagVisualState state;
            if(visualCache != null && visualCache.TryGetValue(path, out state)) {
                return state;
            }
            state = ComputeVisualStateForPath(path);
            if(state.HasTag && visualCache != null) {
                visualCache[path] = state;
            }
            return state;
        }

        private static TagDefinition GetOrCreateDefinition(string tag) {
            TagDefinition definition;
            if(!tagDefinitions.TryGetValue(tag, out definition) || definition == null) {
                definition = new TagDefinition(tag);
                tagDefinitions[tag] = definition;
            }
            return definition;
        }

        private static TagVisualState ComputeVisualStateForPath(string path) {
            HashSet<string> tags;
            if(tagAssignments.TryGetValue(path, out tags)) {
                return ComputeVisualState(tags);
            }
            return TagVisualState.Empty;
        }

        private static TagVisualState ComputeVisualState(HashSet<string> tags) {
            if(tags == null || tags.Count == 0) {
                return TagVisualState.Empty;
            }
            List<string> ordered = new List<string>(tags);
            ordered.Sort(StringComparer.OrdinalIgnoreCase);
            Color? color = ResolveColor(ordered);
            int hash = ComputeVisualHash(ordered, color);
            return new TagVisualState(true, color, hash);
        }

        private static Color? ResolveColor(IList<string> orderedTags) {
            TagDefinition winner = null;
            foreach(string tag in orderedTags) {
                TagDefinition definition;
                if(!tagDefinitions.TryGetValue(tag, out definition) || definition == null || !definition.Color.HasValue) {
                    continue;
                }
                if(winner == null) {
                    winner = definition;
                    continue;
                }
                if(definition.Priority > winner.Priority) {
                    winner = definition;
                    continue;
                }
                if(definition.Priority == winner.Priority && string.Compare(definition.Name, winner.Name, StringComparison.OrdinalIgnoreCase) < 0) {
                    winner = definition;
                }
            }
            return winner != null ? winner.Color : (Color?)null;
        }

        private static int ComputeVisualHash(IList<string> orderedTags, Color? color) {
            int hash = 17;
            hash = (hash * 31) + (color.HasValue ? color.Value.ToArgb() : 0);
            for(int i = 0; i < orderedTags.Count; i++) {
                string tag = orderedTags[i] ?? string.Empty;
                hash = (hash * 31) + StringComparer.OrdinalIgnoreCase.GetHashCode(tag);
                TagDefinition definition;
                if(tagDefinitions.TryGetValue(tag, out definition) && definition != null) {
                    hash = (hash * 31) + definition.Priority;
                }
            }
            return hash;
        }

        private static bool UpdateVisualCacheForPath(string path, HashSet<string> tags) {
            if(visualCache == null) {
                visualCache = new Dictionary<string, TagVisualState>(StringComparer.OrdinalIgnoreCase);
            }
            TagVisualState oldState;
            visualCache.TryGetValue(path, out oldState);
            TagVisualState newState = ComputeVisualState(tags);
            if(newState.HasTag) {
                visualCache[path] = newState;
            }
            else {
                visualCache.Remove(path);
            }
            return oldState.VisualHash != newState.VisualHash;
        }

        private static void RebuildVisualCache() {
            if(visualCache == null) {
                visualCache = new Dictionary<string, TagVisualState>(StringComparer.OrdinalIgnoreCase);
            }
            else {
                visualCache.Clear();
            }
            foreach(KeyValuePair<string, HashSet<string>> assignment in tagAssignments) {
                if(assignment.Value == null || assignment.Value.Count == 0) {
                    continue;
                }
                visualCache[assignment.Key] = ComputeVisualState(assignment.Value);
            }
        }

        private static void BroadcastTagChanges(TagVisualChangedEventArgs args) {
            TagVisualChangedEventArgs payload = args ?? TagVisualChangedEventArgs.Global;
            try {
                EventHandler<TagVisualChangedEventArgs> handler = TagVisualChanged;
                if(handler != null) {
                    handler(null, payload);
                }
            }
            catch { }
            try {
                InstanceManager.TabBarBroadcast(tabBar => tabBar.RefreshTagVisuals(), true);
            }
            catch { }
        }

        private static string[] SplitLegacyAware(string line) {
            if(line == null) {
                return new string[0];
            }
            string[] parts = line.Split('\t');
            if(parts.Length > 1) {
                return parts;
            }
            int index = -1;
            int runLength = 0;
            for(int i = 0; i < line.Length; i++) {
                if(line[i] != ' ') {
                    continue;
                }
                int j = i;
                while(j < line.Length && line[j] == ' ') {
                    j++;
                }
                runLength = j - i;
                if(runLength >= 2) {
                    index = i;
                    break;
                }
                i = j - 1;
            }
            if(index < 0) {
                return new[] { line };
            }
            string first = line.Substring(0, index);
            string second = line.Substring(index + runLength);
            return new[] { first, second };
        }

        private sealed class TagDefinition {
            internal TagDefinition(string name) {
                Name = name;
            }

            internal string Name { get; private set; }

            internal Color? Color { get; set; }

            internal int Priority { get; set; }

            internal bool HasSerializableData {
                get { return Color.HasValue || Priority != 0; }
            }
        }
    }
}
