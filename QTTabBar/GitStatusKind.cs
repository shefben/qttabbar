using System;

namespace QTTabBarLib {
    internal enum GitStatusKind {
        None = 0,
        Clean,
        Modified,
        Untracked,
        Mixed,
        Unknown
    }
}
