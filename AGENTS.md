# Agent Instructions

This project uses Spec-Driven Development.

Core rules:
- Read `SPEC.md` before changing code.
- For new work, read the relevant file in `Features/`.
- Do not implement behavior not described in `SPEC.md` or the feature spec.
- If behavior changes, update `SPEC.md` and the affected feature spec in the same change.
- Keep feature specs narrow, concrete, and testable.

Execution guardrails:
- Build only the smallest vertical slice required by the current feature spec.
- Prefer explicit acceptance criteria and deterministic behavior over flexible or inferred behavior.
- If a requested behavior is out of scope, update specs first, then implement.
- For this repository, keep file-path handling Windows-first unless specs explicitly expand platform scope.
