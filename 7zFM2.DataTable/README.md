# MAUI DataTable

A general-purpose MAUI table with fixed headers, resizable columns, and one **CollectionView
item per row**, conceptually similar to the WinUI CommunityToolkit Labs DataTable.
CollectionView is MAUI's recommended replacement for the deprecated ListView; the control does
not depend on WinUI or application-specific data types.

## Using the control

Bind `ItemsSource` and `SelectedItem` as before. Populate `DataTable.Columns` with
`DataTableColumn` objects; leave the legacy whole-row `ItemTemplate` unset.
`7zFM2/FileManagerPage.xaml` is the seven-column example.

Each column exposes:

| Property | Meaning |
| --- | --- |
| `Header` | Header text |
| `BindingPath` | Cell binding path, relative to the row item |
| `StringFormat` | Optional MAUI binding string format |
| `CellTemplate` | Optional custom cell template whose root must be a `View`; binding context is the row item |
| `Width` | Requested width in device-independent units; two-way by default |
| `MinWidth` / `MaxWidth` | Resize bounds (default 40 / infinity) |
| `ActualWidth` | Effective width clamped to the bounds; minimum wins for contradictory bounds |
| `CanResize` | Whether the header offers a drag gesture |
| `TextAlignment` | Alignment for default text cells and the header |

Widths must be positive and finite; maximum width also permits positive infinity. Explicit
binding sources can be used for column properties. Column addition, removal, reordering, and
metadata changes rebuild cells only on existing row views. Width changes only relayout cells.
Updates must occur on the UI thread, as for other MAUI controls.

Rows are fixed at 28 DIP and the header at 32 DIP. Cells truncate instead of wrapping. The
header stays fixed during vertical scrolling; header and rows share one horizontal scroll
document. The content height is explicitly bounded to the viewport to preserve virtualization.
Place the table in a finite-height container (for example, a Grid `*` row), not a vertical
StackLayout/ScrollView with unlimited height.

For generated column rows, the list occupies a fixed-width Grid column matching the table's
visible viewport, not the full column sum. Horizontal scrolling counter-offsets that list and
offsets its cells in the opposite direction; the header scrolls with the document as before.
This keeps the vertical scrollbar at the viewport edge while preserving the ancestor scroll
container and column synchronization. New rows read the current offset during layout.
Only cell positions change on a horizontal scroll; the list is not reloaded or remeasured.

`IDataTablePlatformLayout` supplies native viewport metrics, including non-overlay scrollbar
gutters. The horizontal extent includes the vertical gutter so the last column remains fully
reachable. GTK also needs a position-only native repair because its pinned collection handler
bypasses the arrange cache used by `TranslationX`. Native viewport observers are disposed on
handler changes. The legacy whole-row `ItemTemplate` retains its document-wide layout contract;
use generated column rows for the fixed viewport behavior.

Single-tap gestures on generated rows preserve native selection. Two taps on the same item
within 500 ms raise `ItemActivated`. Replacing `ItemsSource` clears selection and tap history.
The application uses replaced snapshot arrays; in-place observable-source selection behavior
is still dependent on the platform handler. The legacy whole-row `ItemTemplate` continues to
work, but such templates own their layout and must call `ReportTap` themselves.

## Native row input

Heads can register `IDataTablePlatformInput` to replace the generated row's MAUI tap source.
The row attaches it after its handler is available and disposes it when that handler changes.
While attached, the MAUI tap recognizer is removed to prevent double reporting. Other heads
continue using the existing MAUI gesture. Both sources call the same shared `ReportTap` path,
using the item from the click rather than a cached item from row creation.

`GtkDataTablePlatformInput` attaches a primary-button `Gtk.GestureClick` to the native item
container when mapped, and removes it when unmapped. It runs in the capture phase before
selection can replace the row, and groups with the native item click gesture so selection's
claim does not suppress the second release. It never claims/denies sequences itself.

The adapter rejects stopped sequences and releases outside the row, reports both clicks,
and leaves selection to the native CollectionView. Reporting uses a GTK idle callback (the
pinned dispatcher can run inline). A completed click survives selection-induced row unmapping,
but changing the table's ItemsSource invalidates old reports. Disposal and rebinding cancel
pending reports; press/release across different bound items is rejected. Headers are excluded.

This is a row-click bridge, not a complete file-manager input controller. Ctrl/Shift selection,
keyboard navigation, rubber-band selection and a GTK header-resize fix remain separate work.

## AppKit compatibility

`MacOSDataTablePlatformLayout` is registered as `IDataTablePlatformLayout` in the macOS head.
It repairs the pinned `0.1.0-preview.12.26474.1` backend, which updates native row frames without
MAUI frames and skips existing rows when the viewport width changes. It preserves row Y/height
and selection; it never reloads the source. Shared code explicitly refreshes the horizontal
document extent and child layouts. The adapter also measures the NSScrollView clip areas and
observes their frame changes for scrollbar gutters. The row-frame repair can be removed once
the upstream fixes are present and column alignment, scrolling, and selection are verified.

Weak references track generated rows so the table does not retain every item ever scrolled past.
This virtualizes rows, **not columns**. Each realized row still contains one view per column;
the earlier AppKit per-control performance cost still applies. No smooth-scrolling FPS guarantee
is implied by the bounded view count.

## Validation status and limitations

A formal automated test suite is deferred. Development-time AppKit checks are not a committed
test suite or evidence of cross-platform correctness. Linux and Windows heads build with the
input adapter. A development-time WSLg/X11 probe using native mouse events verified single-click
selection, one activation per double-click, right-click/header exclusion, source replacement,
drag rejection, unmap/remap without duplicate subscriptions, and stale-source report rejection.
The fixed-viewport layout was checked in WSLg with both Wayland and X11, including non-overlay
scrollbars, horizontal endpoints, newly realized rows, window resizing and column shrinking.
The mouse checks also passed after horizontal scrolling. The AppKit changes still need a macOS
build and runtime check (the Windows validation host lacks the macos workload); full-application
and cross-platform interaction checks are not replaced by these focused probes.

Bounded realized-row counts verify virtualization, not interaction latency or frame rate.
Scrolling and column resizing should be evaluated in Release builds, including sustained input
and large scroll movements. Debug builds can be substantially slower even without a debugger.

Sorting, column reorder gestures, width persistence, and a native resize cursor are not part of
this implementation.