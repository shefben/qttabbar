# Explorer Visual Enhancements – Architecture Plan

## Context
The existing codebase already supports per-tag foreground colors for list-view rows and tab captions, maintains tag metadata via `TagManager`, exposes tab grouping state through `TabGroupState`, and surfaces Git repository status text from `GitStatusManager`. However, the current implementation stops short of:

- Allowing users to pick colors while creating or editing tags.
- Applying tag colors to the navigation tree or ensuring consistent precedence across every surface.
- Representing grouped tabs as collapsible "islands" with their own layout, drag/drop semantics, and persistent colored rails.
- Decorating tab icons with visual Git status overlays instead of textual suffixes.

This document captures the architectural plan for extending the platform to cover those gaps while keeping the shell extension resilient.

## 1. Tag Color Selection and Propagation

### 1.1 Data Contracts
- **Tag metadata** – Introduce a `TagDefinition` record that contains the tag name, optional color, and future flags (e.g., default rail color). `TagManager` will internally replace the parallel `tagAssignments` + `tagColors` dictionaries with a single `Dictionary<string, TagDefinition>` plus a `Dictionary<string, HashSet<string>>` for path assignments.  Backwards compatibility is achieved by continuing to write `tagdefs.tsv` until a migration path is defined.
- **Tag color cache** – Extend `TagManager` with a `ConcurrentDictionary<string, TagVisualState>` keyed by normalized paths. Each entry stores the effective text color, last refresh timestamp, and a hash of the contributing tags. Consumers (`QTabItem`, list views, tree view) will query this cache to avoid repeated set-lookups during draw cycles.
- **Notifications** – Replace the current `BroadcastTagChanges()` boolean-only broadcast with a `TagVisualChangedEventArgs` payload that includes affected paths, modified tags, and the new effective color. This allows targeted refreshes instead of invalidating every handle.

### 1.2 UI Surface for Color Picking
- Extend `TagsForm` to show a color picker (Win32 `CHOOSECOLOR` or custom WPF host) whenever a tag is created or edited. Persist the selection through `TagManager.SetTagColor`.
- Provide defaults and accessibility fallbacks (e.g., high-contrast palettes) by centralizing options in a new `TagColorPalette` helper.

### 1.3 Applying Colors in Explorer Surfaces
- **Tabs (`QTabItem`)** – Continue using `UpdateTagColor`, but swap to the cache and respect selection/highlight overrides. When multiple tags apply, enforce deterministic priority: explicit tag-level priority > alphabetical order > fallback color.
- **Main list views (`ExtendedSysListView32`)** – Use the cached color to avoid repeatedly calling into `TagManager`. Implement a `ListViewTagDecorator` responsible for subscribing to `TagVisualChanged` events and invalidating affected items.
- **Navigation tree (`SysTreeView32`)** – Hook `NM_CUSTOMDRAW` via `TreeViewWrapper`. The wrapper will:
  - Map tree nodes to shell items using the existing `INameSpaceTreeControl` reference and `IShellItem.GetDisplayName(SIGDN_DESKTOPABSOLUTEEDITING)`.
  - Query the tag color cache and apply via `CDRF_NEWFONT` or owner-draw to change `clrText`.
  - Maintain a lightweight node-to-path map refreshed on `TVN_GETDISPINFO` to minimize COM round-trips.
- **Selection precedence** – Define shared utilities in `TagVisualState` so all consumers apply the same rules (selected rows use system highlight text, inactive windows dim untagged items when the corresponding option is enabled, etc.).

### 1.4 Persistence and Migration
- Version `tagdefs.tsv` by appending a header (e.g., `#version=2`). When the loader detects v1 files, it will parse existing entries into `TagDefinition` instances with default priorities.
- Introduce regression tests for loading/saving definitions and ensuring colors survive round-trips.

## 2. Tab Group “Island” Representation

### 2.1 Core Concepts
- **Island model** – Create a `TabGroupIsland` class that encapsulates group ID, display color, collapsed state, animation progress, and the ordered list of member tab IDs. Persist this structure in `TabGroupState` so that reopening Explorer sessions restores island layout.
- **Color source** – Allow groups to reuse tag colors by default (when all members share the same dominant tag) but store an explicit `Color? RailColor` on `TabGroupIsland` for manual overrides.

### 2.2 Layout Pipeline Changes
- Extend `QTabControl` with a pre-layout pass that walks the tab order, clusters consecutive tabs belonging to the same island, and computes bounding rectangles for:
  - The expanded island background rectangle.
  - The collapsed rail rectangle (a narrow strip with the same width as a tab header when hidden).
- Refactor `DrawTab` so actual tab rendering becomes a child operation inside `DrawIsland`, ensuring consistent padding and z-order. Collapsed islands paint only the rail.

### 2.3 Interaction Model
- **Collapse/expand** – Introduce hit-testing for the rail rectangle. Clicking toggles the `Collapsed` flag and triggers an animation that slides tabs in/out using existing timer infrastructure (`QAnimator`).
- **Drag/drop membership** – Extend `HandleMouseMove` and `OnTabDrop` so that when a dragged tab hovers over an island rail, a drop indicator appears. Dropping adds the tab’s group ID to the island (or removes it when dropped outside all islands). Update `TabGroupIsland` and broadcast via `TabGroupStateChanged` events.
- **Multiple islands** – Support multiple `TabGroupIsland` instances by keeping them in an ordered list aligned with the tab strip. Persist their order and collapsed state.

### 2.4 Persistence & Backwards Compatibility
- Store island metadata alongside existing group settings in the options file. When loading older configurations without islands, initialize a default island per group with `Collapsed=false` and `RailColor=null`.
- Ensure serialization occurs through the same option-saving pipeline already used by `TabGroupState` to minimize risk.

## 3. Git Status Icon Overlays

### 3.1 Data and Rendering Contracts
- Introduce a `GitStatusKind` enum (`Clean`, `Modified`, `Staged`, `Untracked`, `Conflict`, `Unknown`). `GitStatusManager` publishes `(path, GitStatusKind)` updates on its dispatcher thread.
- Create a `GitIconOverlayManager` responsible for:
  - Maintaining base icon indices for each tab (`QTabItem.BaseIconIndex`).
  - Generating overlay bitmaps (16×16, 24×24, 32×32) tinted per status color (green, yellow, red, etc.).
  - Registering composited images inside `QTUtility.ImageListGlobal` and returning the composite index.

### 3.2 Update Flow
1. `GitStatusManager` detects a status change and raises an event with the `GitStatusKind`.
2. `GitIconOverlayManager` computes the overlay key (e.g., `baseIndex|Modified`). If not cached, it lazily composes a new bitmap and inserts it into the shared image list using UI-thread marshaling to satisfy Win32 requirements.
3. `QTabItem` updates its `ImageIndex`/`ImageKey` on the UI thread and invalidates the tab for repaint.

### 3.3 Performance and Reliability
- Batch updates by coalescing multiple status changes onto the UI thread dispatcher (`SynchronizationContext.Post`).
- Dispose of generated bitmaps when Explorer exits to prevent leaking handles.
- Provide feature toggles in options for users who prefer textual badges.

## 4. Cross-Cutting Concerns

- **Threading** – All UI updates must occur on the Explorer UI thread. Use the existing `ExplorerInterop.RunOnUiThread` helpers where possible.
- **Testing hooks** – Add unit tests for new serialization logic and overlay key caching. Use integration harnesses (e.g., `TestHost`) to validate tree view coloring and island layout via automation IDs.
- **Telemetry/logging** – Extend `QTUtility2.log` calls with identifiers (`[TagColor]`, `[Island]`, `[GitOverlay]`) to ease troubleshooting.

## 5. Implementation Phasing

1. Refactor `TagManager` to use the new data contracts, expose color picking UI, and wire caches to tabs/list views.
2. Implement tree view custom draw and ensure performance remains acceptable (measure paint times before/after).
3. Introduce the `TabGroupIsland` model, modify layout/draw pipelines, and implement collapse/expand interactions.
4. Layer in drag/drop membership and persistence for islands.
5. Build the `GitIconOverlayManager`, integrate with `QTabItem`, and expose configuration options.

Each milestone should ship behind hidden options or feature flags to limit blast radius during rollout.
