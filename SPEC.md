# ModLoader Specification

## Product Baseline
ModLoader is a lightweight Windows-first desktop interface for managing Doom source-port launch inputs.

For each launch profile, users provide:
- One source-port input from an ordered source-port list.
- One IWAD input from an ordered IWAD list.
- Zero or more mod inputs from an ordered mod list.

Feature 005 added launch execution for the pre-profile single-selection workflow.
Feature 008 makes saved profiles the only launchable unit while keeping Source Ports, IWADs, and Mods as shared library collections.

This repository follows feature-scoped delivery. Behavior is only guaranteed when specified in feature specs under `Features/`.

## Feature Index
- Feature 001: Drag-and-drop source-port, IWAD, and mod input lists.
  - Authoritative spec: `Features/001-drop-zones.md`.
- Feature 002: Full-border drop zones and selectable IWAD/Mod rows.
  - Authoritative spec: `Features/002-border-drop-and-row-selection.md`.
- Feature 003: Config persistence and startup recovery.
  - Includes persisted selection state for IWAD and Mod rows.
  - Authoritative spec: `Features/003-config-persistence-and-recovery.md`.
- Feature 004: Fixed command preview footer and selection-synchronized mod ordering.
  - Adds generated launch-argument preview (`-iwad`, `-file`) using filenames-only with wrapped footer display, and derives Mod display ordering from current selected-mod sequence instead of persisting shared-library reorders.
  - Authoritative spec: `Features/004-fixed-command-preview-and-selection-order.md`.
- Feature 005: Fixed header and launch execution.
  - Adds a fixed top header with title/label and right-aligned launch action.
  - Executes source port with generated full-path `-iwad` / `-file` arguments from current selection state.
  - Authoritative spec: `Features/005-fixed-header-and-launch-execution.md`.
- Feature 006: Section collapse layout and row interaction states.
  - Aligns Source Port/IWAD/Mod section headers with inline collapse actions and row-level `Remove` actions using shared right-hand action-column layout patterns.
  - Adds whole-section collapse behavior plus distinct row `hover`, `selected`, and `selected+hover` visuals for light/dark themes.
  - Authoritative spec: `Features/006-row-actions-clear-all-and-row-states.md`.
- Feature 007: Source-port list parity and Mod `.zip` support.
  - Replaces single active source-port behavior with ordered source-port list behavior and single-select source-port row state.
  - Expands Mod allowlist to include `.zip`.
  - Updates command preview to include selected source-port filename before `-iwad` / `-file` segments.
  - Authoritative spec: `Features/007-source-port-list-and-mod-zip.md`.
- Feature 008: Selection-based profile workspace.
  - Adds saved profiles as the primary launch model in a two-pane workspace: a pinned profile list on the left and independently scrolling shared file library on the right, with `New Profile` in the right-pane library header.
  - Profiles persist source port, IWAD, and ordered mod references to the shared library and are the only launchable unit.
  - Profile rows expose explicit `Rename` and `Delete` actions instead of double-click rename, and outside-click rename exit cancels rather than saves.
  - Profile edits auto-save immediately through file-library selection changes; launch requires a selected valid saved profile.
  - Authoritative spec: `Features/008-profile-management.md`.

## Scope Boundary For Feature 001
Feature 001 provides in-memory state management and UI interactions only. It does not include:
- Persistence to disk (provided later by Feature 003).
- Launch execution (provided later by Feature 005).
- Profile management.
- Recursive directory traversal.
