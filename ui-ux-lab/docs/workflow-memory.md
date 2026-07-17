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
