# Rotatonator v1.5 Release Notes (Draft)

## Highlights

- Added append macro support for CH detection via inline token format:
  - `rotat:<number_in_chain>, <target>`
  - Example: `rotat:3, %t`
- Added **Export append macro** button to generate and copy the correct token for the current player position.
- Added in-app help text showing append macro usage.
- Added log utility controls in the main window:
  - Live log monitor indicator
  - Log file size display
  - Archive + truncate button for large logs
- Added optional NPC target audio warning in audio alert configuration.

## Append Macro Behavior

- Parser scans every incoming log line for `rotat:<number>, <target>` (space after comma is allowed).
- The `<number>` maps to healer position in chain (`1` = first healer).
- Works when appended to existing CH macros or as a standalone line without changing existing prefix workflows.

## Stability and Hardening

- Performed post-rewind stabilization of `MainWindow.xaml.cs`.
- Removed duplicate declarations and restored required handlers.
- Restored monitor state UI updates used by Start/Stop monitoring flow.
- Hardened append token matching to avoid malformed/partial token matches.

## Docs Updated

- `README.md`
  - Added append macro feature and usage guidance
  - Added log utility controls in feature list
- `CHANGELOG.md`
  - Added `1.5.0 - Unreleased` section summarizing new features, changes, and fixes

## Upgrade Notes

- Existing CH prefix detection remains supported.
- You can now append `rotat:<position>, %t` to your current macro line or use it standalone to opt into alternate detection.

## Validation

- `dotnet build` passes from workspace root.
- `dotnet build` passes from project folder (when executable is not locked by a running process).
