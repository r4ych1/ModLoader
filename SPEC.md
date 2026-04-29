# ModLoader Specification

## Product Baseline
ModLoader is a lightweight Windows-first desktop interface for managing Doom source-port launch inputs.

For each launch profile (future feature), users will provide:
- A source-port executable.
- One IWAD input from an ordered IWAD list.
- Zero or more mod inputs from an ordered mod list.

This repository follows feature-scoped delivery. Behavior is only guaranteed when specified in feature specs under `Features/`.

## Feature Index
- Feature 001: Drag-and-drop source-port, IWAD, and mod input lists.
  - Authoritative spec: `Features/001-drop-zones.md`.
- Feature 002: Full-border drop zones and selectable IWAD/Mod rows.
  - Authoritative spec: `Features/002-border-drop-and-row-selection.md`.
- Feature 003: Config persistence and startup recovery.
  - Includes persisted selection state for IWAD and Mod rows.
  - Authoritative spec: `Features/003-config-persistence-and-recovery.md`.

## Scope Boundary For Feature 001
Feature 001 provides in-memory state management and UI interactions only. It does not include:
- Persistence to disk (provided later by Feature 003).
- Launch execution.
- Profile management.
- Recursive directory traversal.
