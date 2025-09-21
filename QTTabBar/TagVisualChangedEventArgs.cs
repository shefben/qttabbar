using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QTTabBarLib {
    internal sealed class TagVisualChangedEventArgs : EventArgs {
        private static readonly IList<string> EmptyList = new ReadOnlyCollection<string>(new string[0]);

        internal static readonly TagVisualChangedEventArgs Global = new TagVisualChangedEventArgs(true, EmptyList, EmptyList, null, null);

        private readonly HashSet<string> pathLookup;
        private readonly HashSet<string> tagLookup;
        private readonly IList<string> paths;
        private readonly IList<string> tags;

        private TagVisualChangedEventArgs(bool requiresFullRefresh, IList<string> paths, IList<string> tags, HashSet<string> pathLookup, HashSet<string> tagLookup) {
            RequiresFullRefresh = requiresFullRefresh;
            this.paths = paths ?? EmptyList;
            this.tags = tags ?? EmptyList;
            this.pathLookup = pathLookup;
            this.tagLookup = tagLookup;
        }

        internal bool RequiresFullRefresh { get; private set; }

        internal IList<string> Paths {
            get { return paths; }
        }

        internal IList<string> Tags {
            get { return tags; }
        }

        internal bool AffectsPath(string path) {
            if(RequiresFullRefresh) {
                return true;
            }
            if(string.IsNullOrEmpty(path) || pathLookup == null) {
                return false;
            }
            return pathLookup.Contains(path);
        }

        internal bool MentionsTag(string tag) {
            if(RequiresFullRefresh) {
                return true;
            }
            if(string.IsNullOrEmpty(tag) || tagLookup == null) {
                return false;
            }
            return tagLookup.Contains(tag);
        }

        internal static TagVisualChangedEventArgs ForPaths(IEnumerable<string> paths, IEnumerable<string> tags) {
            List<string> pathList = new List<string>();
            HashSet<string> pathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if(paths != null) {
                foreach(string candidate in paths) {
                    string trimmed = (candidate ?? string.Empty).Trim();
                    if(trimmed.Length == 0) {
                        continue;
                    }
                    if(pathSet.Add(trimmed)) {
                        pathList.Add(trimmed);
                    }
                }
            }

            List<string> tagList = new List<string>();
            HashSet<string> tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if(tags != null) {
                foreach(string candidate in tags) {
                    string trimmed = (candidate ?? string.Empty).Trim();
                    if(trimmed.Length == 0) {
                        continue;
                    }
                    if(tagSet.Add(trimmed)) {
                        tagList.Add(trimmed);
                    }
                }
            }

            if(pathList.Count == 0 && tagList.Count == 0) {
                return Global;
            }

            IList<string> readOnlyPaths = pathList.Count > 0 ? (IList<string>)new ReadOnlyCollection<string>(pathList.ToArray()) : EmptyList;
            IList<string> readOnlyTags = tagList.Count > 0 ? (IList<string>)new ReadOnlyCollection<string>(tagList.ToArray()) : EmptyList;
            HashSet<string> lookupPaths = pathSet.Count > 0 ? pathSet : null;
            HashSet<string> lookupTags = tagSet.Count > 0 ? tagSet : null;

            return new TagVisualChangedEventArgs(false, readOnlyPaths, readOnlyTags, lookupPaths, lookupTags);
        }
    }
}
