# Feature 005 - Fixed Header and Launch Execution

## Goal
Provide a fixed top header with persistent title/label visibility and a right-aligned Launch action that executes the selected source port with deterministic full-path arguments.

## In Scope
- Fixed, non-scrolling top header.
- Header content:
  - Current title text.
  - Current label text.
  - Right-aligned `Launch` button.
- Launch enablement gate:
  - Enabled only when one source-port row is selected and one IWAD is selected.
- Launch command generation using full paths:
  - `-iwad <selectedIwadFullPath>`
  - Optional `-file <selectedModFullPath...>` in selected Mod sequence order.
- Launch execution through a testable process-launch abstraction.
- Non-blocking warning banner when launch fails.

## Out Of Scope
- Any change to footer command preview format from Feature 004.
- Changing selection semantics from prior features.
- Profile management.
- Retry workflows or modal error dialogs.

## Definitions
- Launch readiness:
  - Selected source-port path is present.
  - Selected IWAD path is present.
- Ordered Mods:
  - The existing selected Mod sequence order.
- Launch arguments:
  - Argument tokens passed to process start, using full normalized absolute paths.

## Rules
### Fixed Header Layout
- Header remains visible while main content scrolls.
- Launch button is visually right-aligned in the header row.

### Launch Gating
- Launch button is disabled when no source-port row is selected.
- Launch button is disabled when no IWAD is selected.
- Launch button is enabled only when both launch-readiness conditions are satisfied.

### Launch Argument Construction
- Executable path is the selected source-port row full path.
- Always include exactly one IWAD segment when launching:
  - `-iwad <selectedIwadFullPath>`
- Include `-file` only when one or more Mods are selected:
  - `-file <selectedMod1FullPath> <selectedMod2FullPath> ...`
- Selected Mod argument order must match selected Mod sequence exactly.

### Launch Failure Feedback
- If launch execution fails, show a non-blocking warning banner in the window.
- Failure warning does not block further interaction.

### Preview Compatibility
- Feature 004 preview behavior remains unchanged:
  - Preview continues to render filename-only tokens with existing quoting rules.
- Launch execution uses full paths independently from preview rendering.

## Acceptance Criteria
### Fixed header visibility
Given content long enough to scroll
When user scrolls the main content area
Then header title/label and Launch button remain visible at the top.

### Launch gating
Given no selected source-port row or no selected IWAD
When UI is rendered
Then Launch button is disabled.
And given one source-port row is selected and one IWAD is selected
Then Launch button is enabled.

### Launch command uses full paths
Given source port, selected IWAD, and selected Mods
When Launch is triggered
Then process start receives executable as source-port full path.
And arguments include `-iwad <selectedIwadFullPath>`.
And arguments include `-file` with selected Mod full paths in selected sequence order.

### Launch without selected Mods
Given source port and selected IWAD with no selected Mods
When Launch is triggered
Then arguments include `-iwad <selectedIwadFullPath>` only.
And no `-file` segment is included.

### Non-blocking failure warning
Given launch is triggered and process start fails
When failure is raised by launcher
Then warning banner is shown with a failure message.
And the app remains interactive.
