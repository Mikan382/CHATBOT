# Workflow Memory

This file stores repeatable project-specific recovery knowledge. Add an entry when a problem is diagnosed and fixed; do not rely on chat history alone.

## Entry format

```md
### Short title
- Symptom:
- Cause:
- Durable fix:
- Verification:
- Reuse when:
```

## Stitch session continuity

- Record the stable Stitch project URL and current screen/frame in `.state/tasks/ui-ux-lab-20-phases/repos/CHATBOT.md` after every meaningful design step.
- Prefer resuming the recorded project URL over creating a new project.
- Browser tab identifiers are temporary hints, not durable state.
- Save screenshots, exports, and evaluation notes under `ui-ux-lab/docs/stitch-evidence/` with stable filenames.
- If copy-as-code fails, capture visual evidence and use View Code only as supporting evidence; generated code is never the production source of truth.

### Stitch mobile drifted away from the real chat contract
- Symptom: the first mobile frame kept navigation dominant and added attachments, notifications, snippets, media cards, PDF summaries, and quiz actions.
- Cause: Stitch filled unspecified mobile space with generic assistant-product patterns even though the desktop anatomy was acceptable.
- Durable fix: refine inside the same project with an explicit allowlist of real contracts, an explicit denylist of invented actions, and separate conversation, session-drawer, quota, and disconnected mobile frames.
- Verification: the refined canvas contains conversation-first mobile, a dedicated session drawer, quota/disconnected state, a dismissible tablet drawer, and no implementation dependency on the invented actions.
- Reuse when: a generated design looks polished but introduces controls that have no matching route, API, state owner, or role contract.

### Browser screenshot clips use CSS pixels while saved screenshots use device pixels
- Symptom: direct browser clip captures for tablet and mobile were blank or offset even though the full screenshot contained the frames.
- Cause: clip coordinates used the saved image's device-pixel dimensions while browser clip coordinates were CSS pixels.
- Durable fix: save one full overview first, then crop the durable PNG using its actual pixel dimensions when stable per-frame evidence is needed.
- Verification: the three saved frame images contain the intended desktop, tablet, and mobile regions.
- Reuse when: a high-DPI browser screenshot is exactly twice the expected viewport size.
