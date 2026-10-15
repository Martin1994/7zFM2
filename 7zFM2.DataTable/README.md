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

Single-tap gestures on generated rows preserve native selection. Two taps on the same item
within 500 ms raise `ItemActivated`. Replacing `ItemsSource` clears selection and tap history.
The application uses replaced snapshot arrays; in-place observable-source selection behavior
is still dependent on the platform handler. The legacy whole-row `ItemTemplate` continues to
work, but such templates own their layout and must call `ReportTap` themselves.

## AppKit compatibility

`MacOSDataTablePlatformLayout` is registered as `IDataTablePlatformLayout` in the macOS head.
It repairs the pinned `0.1.0-preview.12.26474.1` backend, which updates native row frames without
MAUI frames and skips existing rows when the viewport width changes. It preserves row Y/height
and selection; it never reloads the source. Shared code explicitly refreshes the horizontal
document extent and child layouts. The native adapter can be removed once the upstream fixes
are present and column alignment, scrolling, and selection have been verified without it.

Weak references track generated rows so the table does not retain every item ever scrolled past.
This virtualizes rows, **not columns**. Each realized row still contains one view per column;
the earlier AppKit per-control performance cost still applies. No smooth-scrolling FPS guarantee
is implied by the bounded view count.

## Validation status and limitations

A formal automated test suite is deferred. Development-time AppKit checks are not a committed
test suite or evidence of cross-platform correctness. macOS and Linux heads have been built;
Windows and Linux interactive behavior still requires validation on those operating systems.

Bounded realized-row counts verify virtualization, not interaction latency or frame rate.
Scrolling and column resizing should be evaluated in Release builds, including sustained input
and large scroll movements. Debug builds can be substantially slower even without a debugger.

Sorting, column reorder gestures, width persistence, and a native resize cursor are not part of
this implementation.