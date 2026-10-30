# Fluent UI System Icons

The ten `ic_fluent_*.svg` files in this directory are from
[Microsoft Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons).

- Copyright (c) 2020 Microsoft Corporation.
- License: MIT; the full upstream license text is in [FluentIcons.LICENSE.txt](FluentIcons.LICENSE.txt).
- Verified upstream revision: `463d58b8186f874ab2f51cf0a08b4ff5097c912f`.
  The original nine local SVGs were byte-identical to that revision when checked on 2026-10-23.
  `ic_fluent_arrow_up_48_regular.svg` was also verified byte-identical on 2026-10-25.
  This identifies a verified source, not necessarily the original download revision.
- Original SVG filenames and contents are retained. No upstream build tooling is vendored.
- This attribution applies only to these Fluent SVGs and future images derived from them,
  not to the existing `default_file.png`, `default_folder.png`, or the original application
  icon design under `../AppIcons/`.

## Source and intended use

The application resource stems below are mapped in [icons.inputs](icons.inputs).
The manual `CustomImageGenerator` tool generates `<stem>_light.png` and
`<stem>_dark.png` in `../Themed/`; these binaries are version-controlled and consumed
by the heads without build-time regeneration. The manifest specifies 4x rendering. Light mode preserves the
SVG's colors; dark mode inverts HSL lightness in encoded RGB (L' = 1 - L), preserving
hue, saturation, and alpha. For these gray icons, `#212121` becomes `#DEDEDE`.
The upstream SVGs are unchanged.
These resources are packaged but are not yet used by the UI.

| Source SVG | Application resource stem | Use |
| --- | --- | --- |
| [ic_fluent_document_add_48_regular.svg](https://github.com/microsoft/fluentui-system-icons/blob/463d58b8186f874ab2f51cf0a08b4ff5097c912f/assets/Document%20Add/SVG/ic_fluent_document_add_48_regular.svg) | `toolbar_add` | Add to archive |
| [ic_fluent_folder_arrow_up_48_regular.svg](https://github.com/microsoft/fluentui-system-icons/blob/463d58b8186f874ab2f51cf0a08b4ff5097c912f/assets/Folder%20Arrow%20Up/SVG/ic_fluent_folder_arrow_up_48_regular.svg) | `toolbar_extract` | Extract archive |
| [ic_fluent_checkmark_circle_48_regular.svg](https://github.com/microsoft/fluentui-system-icons/blob/463d58b8186f874ab2f51cf0a08b4ff5097c912f/assets/Checkmark%20Circle/SVG/ic_fluent_checkmark_circle_48_regular.svg) | `toolbar_test` | Test archive |
| [ic_fluent_document_copy_48_regular.svg](https://github.com/microsoft/fluentui-system-icons/blob/463d58b8186f874ab2f51cf0a08b4ff5097c912f/assets/Document%20Copy/SVG/ic_fluent_document_copy_48_regular.svg) | `toolbar_copy` | Copy |
| [ic_fluent_document_arrow_right_24_regular.svg](https://github.com/microsoft/fluentui-system-icons/blob/463d58b8186f874ab2f51cf0a08b4ff5097c912f/assets/Document%20Arrow%20Right/SVG/ic_fluent_document_arrow_right_24_regular.svg) | `toolbar_move` | Move |
| [ic_fluent_delete_48_regular.svg](https://github.com/microsoft/fluentui-system-icons/blob/463d58b8186f874ab2f51cf0a08b4ff5097c912f/assets/Delete/SVG/ic_fluent_delete_48_regular.svg) | `toolbar_delete` | Delete |
| [ic_fluent_info_48_regular.svg](https://github.com/microsoft/fluentui-system-icons/blob/463d58b8186f874ab2f51cf0a08b4ff5097c912f/assets/Info/SVG/ic_fluent_info_48_regular.svg) | `toolbar_info` | Information |
| [ic_fluent_arrow_up_48_regular.svg](https://github.com/microsoft/fluentui-system-icons/blob/463d58b8186f874ab2f51cf0a08b4ff5097c912f/assets/Arrow%20Up/SVG/ic_fluent_arrow_up_48_regular.svg) | `address_up` | Address bar: go to parent directory |
| [ic_fluent_document_48_regular.svg](https://github.com/microsoft/fluentui-system-icons/blob/463d58b8186f874ab2f51cf0a08b4ff5097c912f/assets/Document/SVG/ic_fluent_document_48_regular.svg) | `fallback_file` | Generic file fallback |
| [ic_fluent_folder_48_regular.svg](https://github.com/microsoft/fluentui-system-icons/blob/463d58b8186f874ab2f51cf0a08b4ff5097c912f/assets/Folder/SVG/ic_fluent_folder_48_regular.svg) | `fallback_folder` | Generic folder fallback |

## Distribution notices

- Each head includes `FluentIcons.LICENSE.txt` and this attribution
  under `ThirdPartyNotices/` in output/publish directories, or the AppKit bundle's
  resources. Preserve these files when assembling installers or other distributions.
- MIT requires the copyright and permission notice to accompany copies or substantial
  portions; it does **not** require an About page, startup notice, or on-icon credit.
  An About > Third-party licenses view is an optional way to make the bundled notices
  accessible. If implemented, show the full license rather than only a repository link.
- TODO: verify final release installers retain these bundled notices; optionally expose
  them through an About > Third-party licenses view when that UI is implemented.
- Carry these notices forward when rasterizing, recoloring, or renaming derived icons.
