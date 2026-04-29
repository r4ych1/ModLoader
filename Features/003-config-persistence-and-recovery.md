# Feature 003 - Config Persistence and Startup Recovery

## Goal
Persist launch-input state to disk and recover safely from missing files or invalid config JSON at startup.

## In Scope
- Persist:
  - Source port path
  - Ordered IWAD path list
  - Ordered Mod path list
  - Selected IWAD path (nullable)
  - Ordered selected Mod path list
- Config file location:
  - `AppContext.BaseDirectory\modloader.config.json` (Windows-first).
- Immediate persistence after all state mutations:
  - add
  - remove
  - clear
  - selection toggle
- Startup sanitation of missing files:
  - Clear source port when its file no longer exists.
  - Remove IWAD/Mod entries whose files no longer exist.
  - Persist cleaned state immediately when sanitation changes state.
- Fault-tolerant config load:
  - Corrupt or invalid JSON falls back to empty state.
  - App shows a non-blocking warning.
  - Broken config is renamed to `modloader.config.json.broken.{yyyyMMdd-HHmmss}`.
  - A new empty config is written to `modloader.config.json` automatically.

## Out Of Scope
- Launch execution.
- Profile management.
- Retry or modal blocking recovery UI.

## Definitions
- Canonical config path:
  - `Path.Combine(AppContext.BaseDirectory, "modloader.config.json")`.
- Empty state config:
  - Source port: `null`
  - IWAD list: empty
  - Mod list: empty
  - Selected IWAD path: `null`
  - Selected Mod list: empty
- Non-blocking warning:
  - UI warning text that does not prevent normal interaction.

## Rules
### Persistence Triggering
- Persist state immediately after successful add/remove/clear operations.
- Persist state immediately after IWAD/Mod selection toggle operations.
- Persisted ordering must match in-memory ordering.
- Persisted paths must be normalized absolute paths.

### Startup Load and Recovery
- If config file does not exist:
  - Start with empty state.
  - No warning shown.
- If config JSON is valid:
  - Load values into state.
  - Apply missing-file sanitation before first UI interaction.
  - If sanitation removed/cleared values, write sanitized config immediately.
- If config JSON is invalid/corrupt:
  - Start with empty state.
  - Show non-blocking warning.
  - Rename broken file using required timestamp suffix.
  - Write new empty config at canonical config path.

### Missing File Sanitation
- Source port:
  - If path is set but file does not exist, clear it.
- IWAD and Mod lists:
  - Remove entries whose file paths do not exist.
  - Preserve relative ordering of remaining entries.
- Selected IWAD:
  - If selected path is not present in sanitized IWAD list, clear it.
- Selected Mods:
  - Remove selected paths not present in sanitized Mod list.
  - Preserve relative ordering of remaining selected paths.
- If sanitation changed source/IWAD/Mod values or selection values, persist sanitized state immediately.

## Acceptance Criteria
### Immediate persistence on mutation
Given existing application state
When a source port, IWAD, or Mod is added, removed, source port is cleared, or IWAD/Mod selection is toggled
Then `modloader.config.json` is updated immediately to match current in-memory state.

### Missing file sanitation at startup
Given `modloader.config.json` contains one or more file paths that no longer exist
When the app starts
Then missing source port value is cleared and missing IWAD/Mod entries are removed.
And selected IWAD/Mod paths that are no longer present are removed from selection state.
And sanitized state is persisted immediately to `modloader.config.json`.

### Fault-tolerant invalid JSON load
Given `modloader.config.json` contains invalid JSON
When the app starts
Then app initializes with empty state.
And a non-blocking warning is shown.
And broken file is renamed to `modloader.config.json.broken.{yyyyMMdd-HHmmss}`.
And a new empty `modloader.config.json` is created automatically.

### Valid JSON load retains deterministic ordering
Given valid `modloader.config.json` with ordered IWAD and Mod lists, and ordered selected Mod list
When the app starts
Then loaded in-memory IWAD and Mod lists preserve persisted order.
And loaded in-memory selected Mod paths preserve persisted order.

### Selection round-trip persistence
Given valid `modloader.config.json` with selected IWAD and selected Mod paths that exist in loaded lists
When the app starts and no sanitation is required
Then selected IWAD and selected Mod paths are restored exactly from config.
