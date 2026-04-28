# Feature 001 - Drag-and-Drop Source Port, IWAD, and Mod Inputs

## Goal
Provide a minimal, testable vertical slice that stores one source-port executable and ordered IWAD and Mod lists using drag-and-drop.

## In Scope
- Three drop zones:
  - Source Port
  - IWAD
  - Mod
- In-memory state only.
- Manual clear action for source port.
- Individual remove action for IWAD and Mod entries.

## Out Of Scope
- Persistence.
- Launching the source port.
- Multiple profiles.
- Recursive directory scanning.

## Definitions
- Normalized absolute path:
  - Convert to absolute path.
  - Compare case-insensitively (Windows-first semantics).
- First-add order:
  - Entries appear in the order they are first successfully added.
- Drop processing order:
  - The order files are encountered by the drop handler while enumerating dropped items.

## Rules
### Source Port Drop Zone
- Accept dropped files only.
- Valid extension allowlist: `.exe`.
- If multiple valid `.exe` files are accepted in one drop, the last valid accepted file in drop processing order becomes the active source port.
- Ignore dropped directories.

### IWAD Drop Zone
- Valid extension allowlist: `.wad`, `.pk3`, `.iwad`, `.ipk3`, `.ipk7`, `.pk7`.
- Files dropped directly are evaluated against the allowlist.
- Dropped directories are treated as file sources:
  - Enumerate only top-level files (`SearchOption.TopDirectoryOnly`).
  - Validate discovered files with the IWAD allowlist.
  - Add valid files in first-seen order.
- Do not add a dropped directory path itself as an IWAD entry.
- Ignore duplicates by normalized absolute path (case-insensitive).

### Mod Drop Zone
- Valid extension allowlist: `.wad`, `.pwad`, `.pk3`, `.pk7`, `.ipk3`, `.ipk7`, `.pkz`.
- Files dropped directly are evaluated against the allowlist.
- Dropped directories are treated as file sources:
  - Enumerate only top-level files (`SearchOption.TopDirectoryOnly`).
  - Validate discovered files with the Mod allowlist.
  - Add valid files in first-seen order.
- Do not add a dropped directory path itself as a Mod entry.
- Ignore duplicates by normalized absolute path (case-insensitive).

## Acceptance Criteria
### Source-port validation
Given dropped files with mixed extensions
When source-port drop zone processes the drop
Then only `.exe` files are accepted and the final accepted `.exe` is stored.

### IWAD validation and dedupe
Given dropped files with mixed extensions and repeated IWAD paths
When IWAD drop zone processes the drop
Then only `.wad`, `.pk3`, `.iwad`, `.ipk3`, `.ipk7`, and `.pk7` files are considered and duplicates are ignored.

### Mod validation and dedupe
Given dropped files with mixed extensions and repeated Mod paths
When Mod drop zone processes the drop
Then only `.wad`, `.pwad`, `.pk3`, `.pk7`, `.ipk3`, `.ipk7`, and `.pkz` files are considered and duplicates are ignored.

### Manual clear and remove
Given existing state in source port, IWAD list, and Mod list
When user clears source port or removes individual IWAD/Mod entries
Then state updates deterministically in-memory without affecting unrelated entries.
