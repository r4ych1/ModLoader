# Feature 006 - Row Action Alignment, Section Clear-All, and Row Interaction States

## Goal
Align row-level and section-level actions into a shared right-hand action-column pattern, add deterministic Source Port/IWAD/Mod clear-all actions, and provide distinct hover/selected/selected+hover row visuals in light and dark themes.

## In Scope
- Row-level `Remove` button alignment for IWAD and Mod rows.
- Row-level `Remove` button alignment for Source Port, IWAD, and Mod rows.
- Section-level collapse / expand toggle for Source Port list.
- Section-level collapse / expand toggles for IWAD and Mod lists.
- Section-level `Clear All` action for Source Port list.
- Section-level `Clear All` actions for IWAD and Mod lists.
- Clear-all enablement rules based on list emptiness.
- Persisted collapse state for Source Port, IWAD, and Mod sections.
- Row visual states:
  - hover
  - selected
  - selected + hover
- Theme-aware row-state styling for both light and dark variants.

## Out Of Scope
- Changes to source-port clear behavior.
- Changes to file allowlists, dedupe, or drop processing rules.
- Changes to selection semantics from prior features.
- Changes to command-preview argument format or launch behavior.

## Definitions
- Shared right-hand action column:
  - The single right-side action column used by row-level `Remove` buttons within a section.
  - The section header action group for that section must align to this same column, with `Collapse` / `Expand` immediately left of `Clear All`.
- Section descriptor row:
  - The horizontal row containing allowed-file/descriptive text for a section.
- Selected + hover state:
  - The visual state when a row is selected and currently pointer-hovered.
- Collapsed section:
  - A section state where the drop zone, descriptive text, and section-header actions remain visible, but the section's input rows are hidden.

## Rules
### Row-Level Remove Alignment
- Each IWAD/Mod row `Remove` button is right-aligned to the section's shared right-hand action column.
- Row `Remove` buttons remain vertically centered within each row.
- Existing remove behavior is unchanged.

### Section Collapse / Expand
- Source Port, IWAD, and Mod sections each provide a section-header toggle button.
- The toggle button label is `Collapse` when that section's rows are currently visible.
- The toggle button label is `Expand` when that section's rows are currently hidden.
- The toggle button appears immediately to the left of the section `Clear All` button.
- Activating the toggle hides or shows only that section's input rows.
- A collapsed section continues to show its section descriptor row and remains an active drop target.
- Collapse state is persisted independently for Source Port, IWAD, and Mod sections.
- Collapse state is shared-library UI state and is not tied to any selected profile.
- Configs that do not contain persisted collapse fields load with all three sections expanded.

### Source Port Clear Layout
- Source Port section provides a `Clear All` action using the existing button label.
- Activating Source Port `Clear All` removes all source-port entries.
- Source Port `Clear All` appears on the same horizontal row as Source Port descriptive/allowed-file text.
- Source Port `Clear All` aligns to the same shared right-hand action column pattern used by row-level `Remove` buttons in Source Port.
- Source Port `Clear All` is disabled when no source-port entries exist.
- Source Port `Clear All` is not rendered on a separate line below Source Port descriptive text.

### IWAD Clear All
- IWAD section provides a `Clear All` action using the existing button label.
- Activating IWAD `Clear All` removes all IWAD entries.
- IWAD `Clear All` appears on the same horizontal row as IWAD descriptive/allowed-file text.
- IWAD `Clear All` aligns to the same shared right-hand action column as IWAD row `Remove` buttons.
- IWAD `Clear All` is disabled when no IWAD entries exist.

### Mod Clear All
- Mod section provides a `Clear All` action using the existing button label.
- Activating Mod `Clear All` removes all Mod entries.
- Mod `Clear All` appears on the same horizontal row as Mod descriptive/allowed-file text.
- Mod `Clear All` aligns to the same shared right-hand action column as Mod row `Remove` buttons.
- Mod `Clear All` is disabled when no Mod entries exist.

### Persistence and Selection Consistency
- IWAD/Mod clear-all actions persist immediately like other mutations.
- Clearing IWAD entries clears selected IWAD state when present.
- Clearing Mod entries removes all selected Mod paths.
- Existing add/remove/selection persistence behavior remains unchanged.

### Row Interaction Visual States
- Hovering a non-selected row shows a visible hover state.
- Selecting a row shows a persistent selected state.
- Hovering a selected row shows an additional visual indication beyond selected-only state.
- Selected rows remain clearly selected while hovered.
- Hover, selected, and selected+hover states are visually distinct.
- Row-state styling is clearly distinguishable in both light and dark themes.

## Acceptance Criteria
### Shared action-column alignment
Given IWAD and Mod rows with row-level `Remove` buttons
When the section is rendered
Then each `Remove` button is aligned to its section's shared right-hand action column.
And each `Remove` button remains vertically centered in its row.

### Section clear-all behavior and placement
Given IWAD or Mod entries exist
When `Clear All` is activated for that section
Then all entries in that section are removed.
And the `Clear All` button is on the same horizontal line as that section's descriptive/allowed-file text.
And the `Clear All` button is aligned to the same right-hand action column used by row-level `Remove` buttons.
And the `Clear All` button is not rendered on a separate line below the descriptive text.

### Section collapse toggle behavior and placement
Given any Source Port, IWAD, or Mod section is rendered
When the section header is displayed
Then that section shows a `Collapse` or `Expand` button immediately to the left of `Clear All`.
And the section descriptor row remains visible whether the rows are expanded or collapsed.
And activating the toggle hides or shows only that section's rows.

### Source Port clear behavior and placement
Given the Source Port section is rendered
When the section is displayed
Then Source Port `Clear All` appears on the same horizontal line as Source Port descriptive text.
And Source Port `Clear All` is right-aligned using the same section action-column layout pattern as IWAD/Mod clear actions.
And Source Port `Clear All` is not rendered on a separate line below Source Port descriptive text.
And Source Port `Clear All` remains disabled when no source-port entries exist.

### Clear-all empty-state gating
Given no IWAD entries
When IWAD section is rendered
Then IWAD `Clear All` is disabled.
And given no Mod entries
When Mod section is rendered
Then Mod `Clear All` is disabled.

### Persisted collapse state
Given a section has been collapsed or expanded
When the app persists state and restarts
Then each section restores its last saved collapsed or expanded state.
And configs without the new collapse fields start with all sections expanded.

### Row hover and selected visual distinctions
Given an IWAD or Mod row
When row is hovered and not selected
Then hover visual is shown.
And when row is selected and not hovered
Then selected visual is shown.
And when row is selected and hovered
Then selected+hover visual is shown and remains clearly selected.
And hover, selected, and selected+hover are visually distinguishable in light and dark themes.

### Behavioral regression safety
Given existing add/remove/select workflows
When Feature 006 changes are applied
Then existing add/remove/select behaviors continue to function with prior deterministic rules.
