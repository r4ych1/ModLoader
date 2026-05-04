# Feature 008 - Selection-Based Profile Workspace

## Goal
Introduce saved launch profiles as the primary launch model by converting the current screen into a two-pane workspace: saved profiles on the left and the shared Source Port / IWAD / Mod library on the right.

## In Scope
- Saved profile list with single-select toggle behavior.
- Profile creation from current valid library selections using generated default names from the right-pane library header area.
- Inline profile rename initiated by an explicit row action.
- Profile delete with confirmation.
- Immediate profile auto-save when editing a selected profile through library selection changes.
- Persisted `Profiles` and `SelectedProfileId`.
- Profile validity computation based on required launch inputs plus library membership and file existence.
- Launch gating based on selected valid saved profile only.
- Backward-compatible load of existing library lists without auto-migrating a profile from legacy selected fields.

## Out Of Scope
- Save / Save As workflow.
- Detached draft persistence across restart.
- Profile notes, tags, extra launch args, last-played metadata, or separate routes/tabs.
- Automatic selection of another profile after delete.
- Any prompt-based profile naming flow during creation.

## Definitions
- Shared library:
  - The persisted Source Port, IWAD, and Mod collections shown in the right pane.
- Profile:
  - A saved record with stable `Id`, unique display `Name`, one source-port path, one IWAD path, and ordered selected mod paths.
- Selected profile:
  - The nullable saved profile identified by `SelectedProfileId`.
- Detached library state:
  - Current library selections shown when no profile is selected. Detached state is temporary UI state only and is not restored on restart.
- Invalid profile:
  - A saved profile whose current references cannot produce valid launch arguments.

## Rules
### Workspace Layout
- The window becomes a two-pane workspace:
  - Left pane: profile management.
  - Right pane: the existing shared Source Port / IWAD / Mod library.
- The left profile pane remains pinned while the right file-library pane scrolls independently.
- The left profile list provides its own internal scrolling when saved profiles exceed available vertical space.
- The right pane begins with a selected-profile header area that also contains the `New Profile` action.
- `New Profile` is right-aligned within that header area and aligned with the selected profile name header.
- The footer command preview remains visible and reflects current library selections, even when no profile is selected.
- The existing top message/warning area is reused for rename validation and delete confirmation.

### Profile List And Selection
- Left pane shows:
  - saved profile rows
  - invalid-state indicators
  - rename access
  - delete access
- Profile rows are single-select toggle rows:
  - Clicking an unselected row selects it.
  - Clicking a different selected row moves selection to that profile.
  - Clicking the selected row unselects it.
- Selecting a profile hydrates current Source Port / IWAD / Mod selections from that profile.
- Unselecting a profile:
  - sets `SelectedProfileId` to `null`
  - clears current Source Port / IWAD / Mod selections
  - disables Launch
- Double-clicking a profile row has no special behavior beyond the existing row-selection interaction model.

### Profile Creation
- `New Profile` is enabled only when current library selections contain:
  - exactly one selected source port
  - exactly one selected IWAD
- Current selected Mods, if any, are copied into the new profile in current order.
- Clicking `New Profile` immediately creates a new saved profile from current library selections.
- New profile name uses the first available `Profile N` positive integer sequence, filling gaps from deleted profiles.
- New profile creation:
  - generates a new stable `Id`
  - persists immediately
  - selects the new profile
- Creating a new profile while another profile is selected creates a second profile from the current library selections and does not overwrite the existing selected profile.

### Profile Editing And Rename
- When a profile is selected, editing Source Port / IWAD / Mod selections changes that selected profile immediately and persists after each change.
- Auto-save includes transitions into invalid state.
- There is no Save, Save As, dirty state, unsaved-changes prompt, or separate profile-name field.
- Each profile row exposes a `Rename` action immediately to the left of `Delete`.
- Profile rename is available only by the row `Rename` action opening inline rename mode for that row.
- Activating `Rename` selects that profile before opening inline rename mode.
- Rename commit behavior:
  - `Enter` saves if valid.
  - Clicking outside the rename input cancels rename and restores the prior saved name.
  - `Escape` cancels and restores the prior saved name.
- The outside click that cancels rename is consumed and does not also activate the clicked row, button, or other control.
- Rename validity rules:
  - name is required
  - name cannot be empty or whitespace-only
  - name must be case-insensitively unique across all profiles
- Invalid rename attempts:
  - do not change the saved name
  - keep rename mode open for that row
  - show a visible validation message in the message/warning area

### Delete Behavior
- Each profile row exposes delete access.
- Delete requires explicit confirmation in the message area before removal.
- Deleting an unselected profile removes only that saved profile.
- Deleting the selected profile:
  - removes that saved profile
  - sets `SelectedProfileId` to `null`
  - clears current Source Port / IWAD / Mod selections
- Deleting a profile does not automatically select a neighboring profile.

### Validity And Launch
- A valid profile requires:
  - exactly one source port
  - exactly one IWAD
  - zero or more mods
  - preserved mod order
  - every referenced path must exist on disk
  - every referenced path must still exist in the matching shared library collection
- Removing a library item that a profile references is allowed.
- Profiles affected by removed or missing library items remain saved and listed.
- Invalid profiles:
  - show an explicit invalid-state indication
  - remain selectable
  - remain renameable
  - remain editable through the shared library
  - remain auto-saveable
  - are not launchable
- Launch is enabled only when:
  - a saved profile is currently selected
  - that selected profile is valid
- No selected profile always disables Launch, even if current detached library selections are otherwise launch-valid.

### Mod Ordering Context
- Mod row ordering is derived UI state and does not rewrite the shared library collection order during selection toggles.
- When no profile is selected:
  - Mod rows default to alphabetical filename order
  - selected Mods temporarily move to the top in detached selected sequence order
  - remaining unselected Mods stay in alphabetical filename order
  - detached selected-mod ordering is not restored on restart
- When a profile is selected:
  - selected Mods appear first in that profile's `SelectedModPaths` order
  - remaining unselected Mods appear afterward in alphabetical filename order
  - changing Mod selection persists only that selected profile's `SelectedModPaths`

### Persistence And Backward Compatibility
- `LaunchInputsConfig` adds:
  - `Profiles`
  - `SelectedProfileId`
- `ProfileConfig` contains:
  - `Id`
  - `Name`
  - `SourcePortPath`
  - `IwadPath`
  - `SelectedModPaths`
- Canonical Feature 008 saves persist:
  - shared library collections
  - saved profiles
  - nullable `SelectedProfileId`
- Canonical Feature 008 saves do not rely on top-level selected source-port / IWAD / mod fields.
- Backward compatibility requirements:
  - existing Source Ports, IWADs, and Mods are preserved from old configs
  - old selected source-port / IWAD / mod fields do not auto-create a saved profile
  - old configs with no profiles load with zero profiles
  - startup with no valid selected profile begins with no selected profile, cleared library selections, and Launch disabled
- Detached no-profile library selections created during runtime are not restored on restart.
- On startup sanitation:
  - sanitize shared library collections as existing features require
  - preserve broken profiles
  - recompute profile validity from sanitized library membership and file existence

## Acceptance Criteria
### Create profile from current selections
Given one selected source port, one selected IWAD, and any number of selected Mods
When `New Profile` is activated
Then a saved profile is created immediately from the current selections.
And the profile name uses the first available `Profile N`.
And the new profile receives a new stable `Id`.
And the new profile becomes the selected profile.

### Select and unselect profile
Given a saved profile exists
When its row is selected
Then current Source Port / IWAD / Mod selections hydrate from that profile.
And when the selected row is selected again
Then `SelectedProfileId` becomes `null`.
And current Source Port / IWAD / Mod selections are cleared.
And Launch is disabled.

### Edit selected profile through library
Given a saved profile is selected
When Source Port / IWAD / Mod selections change in the shared library
Then the selected profile persists those changes immediately.
And those changes may place the profile into or out of invalid state.

### Explicit rename action
Given a saved profile row exists
When `Rename` is activated for that row
Then that profile becomes selected.
And inline rename mode opens for that row.
And `Enter` saves a valid unique non-empty name.
And outside click or `Escape` restores the previous saved name.
And the outside click does not also activate another control.

### New profile placement
Given the workspace is rendered
When the right-pane selected-profile header area is displayed
Then `New Profile` appears in that header area.
And `New Profile` is right-aligned and aligned with the selected profile name header.
And the left profile pane does not render a second `New Profile` action.

### Rename validation
Given a profile is in rename mode
When the entered name is empty, whitespace-only, or duplicates another profile name case-insensitively
Then the saved name remains unchanged.
And rename mode stays open.
And a visible validation message is shown in the message area.

### Pinned workspace panes
Given the workspace is rendered
When the file library content exceeds available vertical space
Then the right pane scrolls independently.
And the left profile pane remains pinned.
And when the profile list exceeds available height, the profile list scrolls within the left pane.

### Delete selected profile
Given a selected profile exists
When delete is confirmed for that profile
Then the profile is removed from saved profiles.
And `SelectedProfileId` becomes `null`.
And current Source Port / IWAD / Mod selections are cleared.
And no neighboring profile is auto-selected.

### Invalid profile remains repairable
Given a saved profile references a library item that is removed or a file path that no longer exists
When validity is recomputed
Then the profile remains saved and listed.
And it is marked invalid with an explicit reason.
And Launch is disabled while that profile is selected.
And changing library selections while it is selected can repair it and restore launchability.

### Mod ordering by profile context
Given at least three Mod rows and one or more profiles
When no profile is selected
Then Mod rows start in alphabetical filename order.
And when Mods are selected, selected Mods move to the top in detached selected sequence order without changing restart state.
And when a saved profile is selected, Mod rows are ordered by that profile's selected sequence first and alphabetical remainder second.

### Legacy startup without profiles
Given an old config containing shared library lists and old top-level selected fields but no profiles
When the app starts under Feature 008
Then the shared library lists are preserved.
And zero profiles are loaded.
And no profile is selected.
And current library selections start cleared.
And Launch is disabled.
