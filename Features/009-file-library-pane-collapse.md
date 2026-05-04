# Feature 009 - Collapsible File Library Pane

## Goal
Wrap the shared file library in its own right-side workspace section and allow that section to collapse into a narrow strip so the left profile pane can expand into the freed space.

## In Scope
- Pane-level collapse and expand behavior for the right-side file library section.
- A dedicated file-library section header with left-aligned collapse control and title.
- Persistent `IsFileLibraryPaneCollapsed` state across restart.
- Layout changes that let the left profile pane expand when the file library is collapsed.

## Out Of Scope
- Resizable splitters or drag-to-resize pane widths.
- Changes to profile creation, selection, rename, delete, validity, or launch rules.
- Changes to Source Port / IWAD / Mod inner section collapse behavior.

## Definitions
- File library pane:
  - The right-side shared library workspace section that contains the selected-profile header plus Source Port, IWAD, and Mod library controls.
- Collapsed file library strip:
  - The narrow right-edge state that keeps a visible expand control and file-library label while hiding the full library body.

## Rules
### Workspace Layout
- The right-side shared library is rendered inside its own bordered `File Library` section similar in structure to the left profile-management section.
- The file-library section header contains:
  - a top-left collapse control
  - the `File Library` section title
- The selected-profile header area with `New Profile` remains inside the file-library pane body and keeps its existing behavior.
- When the file library is expanded:
  - the workspace uses the existing Feature 008 two-pane arrangement
  - the file library fills its default right-side size
- When the file library is collapsed:
  - the right side reduces to a narrow strip
  - the strip remains visible on the right edge
  - the strip still exposes an expand control
  - the strip still shows the file-library label
  - the left profile pane expands to fill the remaining workspace width
- Collapsing the file library does not move it to the left side and does not overlap the profile pane.

### Behavior Preservation
- File-library pane collapse does not change:
  - selected profile
  - current library selections
  - command preview behavior
  - left-pane row launch availability
  - profile auto-save behavior
  - Source Port / IWAD / Mod inner section collapse state
- Expanding the file library restores the full file-library pane body without resetting its inner section states.

### Persistence
- `LaunchInputsConfig` adds:
  - `IsFileLibraryPaneCollapsed`
- The file-library pane collapse state persists immediately after toggle.
- Saves that do not contain `IsFileLibraryPaneCollapsed` load with the file library expanded by default.

## Acceptance Criteria
### Collapse to narrow strip
Given the workspace is rendered with the file library expanded
When the file-library collapse control is activated
Then the file library collapses into a narrow strip on the right edge.
And the strip still shows an expand control.
And the strip still shows the file-library label.
And the left profile pane expands to fill the remaining workspace width.

### Expand to default size
Given the file library is collapsed
When the expand control is activated
Then the file library returns to its default expanded size.
And the selected-profile header area and shared library controls become visible again.

### Collapse state persists
Given the user collapses or expands the file library
When config persistence occurs and the app restarts
Then the file-library pane restores the last saved collapsed state.

### Existing workspace behavior remains intact
Given any file-library pane state
When the user selects profiles, launches profiles from the left pane, edits library selections, or toggles inner Source Port / IWAD / Mod sections
Then existing Feature 008 profile, launch, and shared-library behavior remains unchanged.
