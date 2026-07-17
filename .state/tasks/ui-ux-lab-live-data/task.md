# Task: ui-ux-lab-live-data

## Target
- Workspace: chatbot-course
- Item: connect the isolated UI lab to real application data
- Desired outcome: `?mode=live` renders authenticated database-backed chat, course, document, benchmark, and user data through the existing application boundary while fixture mode remains available.

## Scope (confirm before editing)
- Repos:
  - CHATBOT -> repos/CHATBOT.md
- Allowed paths / out of scope: `ui-ux-lab/**`, a narrow authenticated API controller under `src/PresentationLayer/Controllers/`, and this task state. No direct DbContext or connection string in the lab; no schema or migration changes.
- Allowed globs: ui-ux-lab/**, src/PresentationLayer/Controllers/UiLabApiController.cs, .state/index.md, .state/tasks/ui-ux-lab-live-data/**
- Cross-harness primary: this harness

## Rollup (derived, not proof)
- Status: implementation verified
- Summary: `?mode=live` now uses authenticated REST and SignalR through the existing application; fixture mode remains the default.

## Progress
- Done: added authenticated UI-lab API projections, role-aware page bindings, live SignalR chat, explicit 401/403/offline states, and the narrow account proxy needed by the existing Razor login form.
- Done: verified the real PresentationLayer against LocalDB. Current database snapshot has 3 users, 1 course, and 0 documents/chat sessions/benchmark runs.
- Done: solution build, UI-lab build, JavaScript syntax, adapter contract, and diff checks pass with zero build warnings/errors.
- Done: implementation commit `5dffd0d` pushed and PR #18 opened against `main`.
- Next step: merge PR #18 and verify the remote main commit.
- Blocked on: none

## Open questions
- None. Live data must pass through application services; the UI lab never owns database credentials.

## Handoff note
- Verify the real PresentationLayer runtime and database are available before claiming live smoke-test success. Offline or unauthenticated states must remain visible and must not silently masquerade as live data.
- The existing LocalDB user rows currently have empty role assignments and the documented bootstrap credential does not match the already-created database. No password or role data was reset because this task does not authorize database mutation.
