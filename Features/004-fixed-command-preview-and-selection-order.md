# Feature 004 - Fixed Command Preview Footer and Selection-Synchronized Mod Ordering

## Goal
Provide a fixed footer that shows a live generated launch-argument preview and keep Mod list ordering synchronized with selected Mod load order.

## In Scope
- Fixed, non-scrolling footer preview text area.
- Preview content includes generated arguments only:
  - `-iwad`
  - `-file`
- Deterministic filename rendering for preview arguments:
  - Render filename tokens only (`Path.GetFileName`) from selected IWAD/Mod paths.
  - Quote a filename only when it contains spaces.
- Wrapped preview text behavior:
  - Preview text wraps within footer bounds to avoid horizontal overflow.
- Mod ordering synchronized to selection sequence:
  - Selected Mods appear first in the same order as selection sequence.
  - Unselected Mods remain after selected Mods in existing relative order.
- Deselection compaction:
  - Removing a Mod from selection removes it from selected sequence and keeps remaining selected sequence order stable.
- Immediate persistence when selection-triggered mod reordering changes state.
- Startup alignment:
  - Restore persisted selection state.
  - Apply selection-synchronized mod ordering.
  - Persist immediately if restored mod ordering differs from required ordering.

## Out Of Scope
- Launch execution.
- Source-port executable prefix in preview text.
- Any keyboard modifier semantics (`Ctrl`, `Shift`) beyond existing behavior.
- Changes to extension allowlists or drop processing rules from prior features.

## Definitions
- Preview arguments:
  - Generated command-line argument string that excludes executable path.
- Selection sequence:
  - Ordered list of currently selected Mod paths based on the sequence of successful selections.
- Selection-synchronized mod ordering:
  - Mod list order used by UI and persisted config where selected Mods appear first in selection sequence, followed by remaining Mods in relative prior order.

## Rules
### Footer Preview Rendering
- Footer preview is visible while main content scrolls.
- Preview text wraps to additional lines within the preview area when needed.
- Preview updates immediately after all relevant state changes:
  - IWAD selection toggle.
  - Mod selection toggle.
  - Operations that remove selected IWAD/Mod rows.
  - Startup sanitation/recovery that changes relevant selection state.
- If no IWAD is selected and no Mods are selected, preview is an empty string.

### `-iwad` Argument
- Include `-iwad <path>` only when an IWAD row is currently selected.
- Selected IWAD contributes exactly one path.

### `-file` Argument
- Include `-file <path1> <path2> ...` only when one or more Mod rows are currently selected.
- Path order in `-file` matches selected Mod sequence exactly.

### Argument Segment Ordering
- Preview argument segments always follow this order:
  1. `-iwad` segment (if present)
  2. `-file` segment (if present)

### Filename Rendering and Quoting
- Preview tokens use filename-only values extracted from selected paths.
- Filenames with spaces are quoted using double quotes.
- Filenames without spaces are not quoted.

### Mod Selection Ordering Behavior
- Selecting an unselected Mod appends that Mod path to the end of selected sequence.
- Deselecting a selected Mod removes only that Mod path from selected sequence.
- After every Mod selection toggle, Mod list order is rebuilt as:
  - all selected Mod paths in selected sequence order
  - then all non-selected Mod paths in their prior relative order

### Persistence
- After Mod selection toggles, persist immediately.
- Persisted `Mods` ordering matches current selection-synchronized mod ordering.
- Startup load persists immediately when selection-synchronized ordering changes persisted Mod order.

## Acceptance Criteria
### Empty preview
Given no selected IWAD row and no selected Mod rows
When the footer preview is shown
Then preview text is empty.

### IWAD-only preview
Given one selected IWAD row and no selected Mod rows
When preview is generated
Then preview contains only `-iwad <iwadPath>`.

### Mod-only preview
Given no selected IWAD row and selected Mod rows
When preview is generated
Then preview contains only `-file <modFileName...>` in selected sequence order.

### Combined preview ordering
Given one selected IWAD row and selected Mod rows
When preview is generated
Then preview is ordered as `-iwad <iwadFileName> -file <modFileName...>`.

### Filename quoting
Given selected IWAD/Mod paths where at least one selected filename contains spaces
When preview is generated
Then only filenames containing spaces are double-quoted.

### Wrapped preview layout
Given preview content long enough to exceed available horizontal footer width
When preview is displayed
Then text wraps within the preview area instead of overflowing horizontally.

### Selection-synchronized Mod list ordering
Given at least three Mod rows
When rows are selected in a specific sequence
Then Mod list order moves selected rows to the top in that same sequence.
And non-selected rows remain afterward preserving their prior relative order.

### Deselection compaction
Given multiple selected Mod rows
When one selected Mod row is deselected
Then that row is removed from selected sequence.
And remaining selected Mod order is unchanged.
And Mod list remains continuous without gaps.

### Immediate persistence of reorder
Given selected Mod rows cause a Mod list reorder
When selection toggle is applied
Then persisted config updates immediately with reordered `Mods`.

### Startup reorder persistence
Given valid config with selected Mod paths and `Mods` order not matching selection-synchronized ordering
When app starts
Then in-memory Mod order is rebuilt to selection-synchronized order.
And reordered state is persisted immediately.
