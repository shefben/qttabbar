using System.Drawing;

namespace QTTabBarLib {
    internal struct TagVisualState {
        internal static readonly TagVisualState Empty = new TagVisualState(false, null, 0);

        internal TagVisualState(bool hasTag, Color? foregroundColor, int visualHash)
            : this() {
            HasTag = hasTag;
            ForegroundColor = foregroundColor;
            VisualHash = visualHash;
        }

        public bool HasTag { get; private set; }

        public Color? ForegroundColor { get; private set; }

        internal int VisualHash { get; private set; }
    }
}
