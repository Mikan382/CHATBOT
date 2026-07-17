# UI/UX Lab - 20 Phase Roadmap

Each phase is one focused commit, one verification checkpoint, and one push to `feature/ui-ux-lab-20-phases`.

| Phase | Deliverable | Verification |
|---:|---|---|
| 01 | Isolation contract, durable task state, and roadmap | Scope diff contains no existing runtime files |
| 02 | Stitch project, repo-grounded prompt, evidence, and feasibility decision | Project URL and winning direction recorded |
| 03 | Vite + React + TypeScript standalone scaffold | Install and production build |
| 04 | Design tokens, typography, spacing, color, and motion foundations | Token preview renders at desktop and mobile |
| 05 | Component workshop and isolated preview routing | Every new primitive has an interactive preview |
| 06 | Buttons, inputs, badges, avatars, tooltips, and focus states | Keyboard and disabled-state checks |
| 07 | Cards, panels, dialogs, dropdowns, tabs, tables, and skeletons | Component-state matrix |
| 08 | Responsive application shell, navigation, account menu, and theme | Desktop, tablet, and mobile shell QA |
| 09 | Sign-in reference screen and authentication boundary | Validation, loading, error, and success previews |
| 10 | Chat workspace frame and course/model controls | Layout and scroll ownership QA |
| 11 | Session rail with search, grouping, rename, and delete states | Keyboard and empty-state QA |
| 12 | Message stream, markdown, citations, generation, and failure states | Long-content and citation QA |
| 13 | Composer, attachments reference, shortcuts, and connection status | Responsive and disabled-state QA |
| 14 | Document library with filters, upload, progress, permissions, and errors | Dense-data and mobile QA |
| 15 | Document detail with extracted text, chunks, embeddings, and indexing timeline | Long-content and partial-data QA |
| 16 | Courses and chapters management reference screens | CRUD state and role-boundary QA |
| 17 | Benchmark dashboard, progress, comparison, charts, and export states | Loading, background-run, and narrow viewport QA |
| 18 | Admin user management and architecture explainer | Table, destructive action, and diagram QA |
| 19 | Existing REST and SignalR adapters with fixture fallback | Contract tests and live-mode smoke test |
| 20 | Accessibility, responsive regression, documentation, merge safety, and final handoff | Build, tests, browser QA, and unchanged current app build |

## Merge contract

The final pull request targets `main`, but its application-code diff remains inside `ui-ux-lab/`. State files may accompany the branch for resumability. The existing `src/` application and solution must build exactly as before.
