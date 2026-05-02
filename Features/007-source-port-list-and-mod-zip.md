# Feature 007 - Source-Port List Parity and Mod `.zip` Support

## Goal
Replace single active source-port behavior with ordered source-port list behavior that matches IWAD row interaction semantics, and expand Mod drop allowlist support to include `.zip`.

## In Scope
- Source Port list behavior:
  - Ordered source-port path list.
  - Source Port row selection with IWAD-parity single-select toggle behavior.
  - Source Port row-level `Remove`.
  - Source Port section-level `Clear All`.
- Launch gating/execution now use selected source-port row.
- Persistence and startup sanitation for source-port list and selected source-port.
- Backward-load compatibility for legacy config using `SourcePortPath`.
- Mod allowlist expansion to include `.zip`.

## Out Of Scope
- Multi-select source-port behavior.
- Automatic source-port selection after drop.
- Changes to IWAD/Mod selection semantics.
- Launch argument path behavior for process execution.

## Definitions
- Source-port list:
  - Ordered set of normalized absolute source-port paths.
- Selected source-port path:
  - Nullable path for exactly one selected source-port row.
- Legacy source-port field:
  - Prior config property: `SourcePortPath` (single value).

## Rules
### Source Port Drop and List Management
- Source Port drop accepts only files with `.exe` extension, evaluated from each dropped file's full path.
- Source Port drop appends valid entries to the source-port list in first-seen order.
- Duplicate source-port paths are ignored by normalized absolute path (case-insensitive).
- Files with identical filenames in different directories are treated as distinct source-port entries.
- Source Port drop ignores directories.
- Source Port list supports row-level remove and section-level clear-all operations.

### Source Port Selection
- Source Port row click toggles single selection:
  - Clicking an unselected row selects it.
  - Clicking a different selected row moves selection to the clicked row.
  - Clicking the selected row clears source-port selection.
- At most one source-port row is selected at a time.
- Removing the selected source-port row clears selected source-port state.
- Clearing all source-port rows clears selected source-port state.

### Launch Readiness and Execution
- Launch requires:
  - One selected source-port row.
  - One selected IWAD row.
- Launch executable path is selected source-port full path.
- Existing IWAD/Mod launch-argument behavior remains unchanged.

### Command Preview
- Preview prepends selected source-port filename token when a source-port row is selected.
- Preview source-port token uses filename-only rendering from selected source-port path.
- Preview source-port token uses the existing double-quote rule when filename contains spaces.
- Existing preview `-iwad` and `-file` segment behavior remains unchanged.
- Preview segment order is:
  1. selected source-port filename token (if present)
  2. `-iwad` segment (if present)
  3. `-file` segment (if present)

### Mod Allowlist Expansion
- Mod valid extension allowlist adds `.zip`.
- `.zip` is accepted for both direct file drops and top-level directory file expansion in Mod drop processing.
- Existing Mod dedupe and ordering rules remain unchanged.

### Persistence and Backward Compatibility
- Persist new source-port fields:
  - `SourcePorts` ordered list.
  - `SelectedSourcePortPath` nullable selection.
- Existing persisted fields for IWAD/Mod and selection remain unchanged.
- On startup sanitation:
  - Remove missing source-port list entries.
  - Clear selected source-port if it is absent after sanitation.
  - Persist immediately when sanitation changes state.
- Legacy compatibility:
  - If config lacks `SourcePorts` and has legacy `SourcePortPath`, load it as a single source-port list entry and selected source-port.
  - New saves write canonical `SourcePorts`/`SelectedSourcePortPath` state.

## Acceptance Criteria
### Source-port multi-add and dedupe
Given dropped paths with multiple valid `.exe` files, non-`.exe` files, and duplicate `.exe` paths
When Source Port drop is processed
Then valid `.exe` paths are appended to source-port list in first-seen order.
And duplicate source-port paths are ignored by normalized absolute path (case-insensitive).

### Source-port same-filename different-path acceptance
Given two dropped source-port files named `gzdoom.exe` in different directories
When Source Port drop is processed
Then both full paths are added to source-port list in drop order.
And both rows are independently selectable.

### Source-port single-select toggle
Given at least two source-port rows
When one row is selected and then a different row is selected
Then only the most recently selected source-port row remains selected.
And when the selected row is selected again
Then no source-port row is selected.

### Launch gating with selected source port
Given source-port rows and IWAD rows exist
When no source-port row is selected or no IWAD row is selected
Then Launch is disabled.
And when one source-port row and one IWAD row are selected
Then Launch is enabled.
And launch executable path equals selected source-port full path.

### Command preview includes selected source port
Given a selected source-port row, selected IWAD row, and selected Mod rows
When command preview is generated
Then preview begins with selected source-port filename token.
And preview ordering is `<sourcePortFileName> -iwad <iwadFileName> -file <modFileName...>`.

### Source-port remove/clear-all selection consistency
Given a selected source-port row
When that row is removed or source-port clear-all is activated
Then source-port selection is cleared.

### Mod `.zip` acceptance
Given a Mod drop contains `.zip`, allowlisted non-`.zip` Mod files, and invalid files
When Mod drop is processed
Then `.zip` and existing allowlisted Mod files are added.
And invalid extensions are not added.

### Backward-load compatibility
Given persisted config with legacy `SourcePortPath` and no `SourcePorts`
When app loads config
Then source-port list contains the legacy source-port path.
And selected source-port equals that path.
