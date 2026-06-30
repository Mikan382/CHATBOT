# Super Nova UI/UX Protocol

Last updated: 2026-07-01

## Objective

Upgrade the PRN222 RAG Chatbot from a functional course-project interface into a polished academic AI workspace. Super Nova quality means the interface feels deliberate at every layer: information architecture, visual system, responsive behavior, state handling, accessibility, and verification.

## Repo And Stack Decision

Target repo: `D:\Coding_learning\CHATBOT`

Current frontend stack:
- ASP.NET Core Razor Pages in `src/PresentationLayer/Pages`.
- Bootstrap 5.1 local assets in `src/PresentationLayer/wwwroot/lib/bootstrap`.
- Custom CSS in `src/PresentationLayer/wwwroot/css/site.css`.
- Browser JavaScript in `src/PresentationLayer/wwwroot/js`.
- SignalR client loaded from CDN on the chat page.
- Chart.js loaded from CDN on the benchmark page.

Library decisions:

| Need | Decision | Reason |
|---|---|---|
| Layout/components | Keep Bootstrap 5.1 plus project CSS | The app is already server-rendered Razor with Bootstrap markup. A React/Tailwind migration would add risk without improving the assignment workflows. |
| Icons | Add Bootstrap Icons as a local/static asset or replace `bi` usage with inline text-free CSS icons | Benchmark already uses `bi` classes but does not load the icon library. Bootstrap Icons matches the Bootstrap stack and avoids a second UI vocabulary. |
| Charts | Keep Chart.js, improve containers and empty/progress states | Benchmark already depends on Chart.js. The issue is layout/state quality, not chart engine fit. |
| Images | No stock/lifestyle image dependency in phase 1 | This is an academic tool, not a marketing product page. Use product-like visual weight through typography, data panels, citations, and document/knowledge motifs first. |
| Motion | CSS-only microinteractions | Keep motion subtle: color, opacity, and transform transitions only. No animation library until a real interaction needs it. |
| Modals/confirmations | Bootstrap modal pattern | Native `confirm()` and `prompt()` break polish and accessibility. Use Bootstrap modal for destructive and benchmark-run decisions. |

## Design Reference Translation

The supplied Apple-style reference is a quality rubric, not a literal product clone.

Use:
- Near-white canvas: `#f5f5f7`.
- White cards with 28px radius and no heavy shadows.
- Ink text: `#1d1d1f`.
- Secondary text: `#707070`.
- Primary action blue: `#0071e3`, rationed to the most important action in each surface.
- Large confident headings where the screen is introductory or decision-oriented.
- Surface depth by color value and spacing, not shadow stacks.

Avoid:
- Dark Bootstrap admin nav as the dominant brand signal.
- Multiple competing button colors in one zone.
- Cards inside cards.
- Dense raw tables on mobile.
- Native browser confirms/prompts.
- Decorative gradients as UI chrome.

## User Jobs

| Surface | Primary user | Job | Super Nova target |
|---|---|---|---|
| Login | Student/Teacher/Admin | Enter the workspace confidently | Premium first impression with clear role/context and trustworthy form. |
| Chat | All roles | Ask course questions and inspect citations | Conversation-first layout, fast course/model controls, readable citations, clear disabled/API states. |
| Documents | Student/Teacher/Admin | Find, upload, inspect indexing | Upload and indexing feel controlled; list is scannable on desktop and readable on mobile. |
| Document details | All roles | Inspect text/chunks/index state | Strong document metadata panel, chunk readability, progress/error clarity. |
| Courses/chapters | Teacher/Admin | Maintain course structure | High-density but calm management tables/forms. |
| Benchmark | Teacher/Admin | Run and compare RAG experiments | Dashboard hierarchy, meaningful empty states, proper run flow, responsive charts. |
| Admin users | Admin | Manage users safely | Risk-aware actions, better grouping, no horizontal clipping. |
| Architecture | Teacher/Admin | Explain system design | Visual architecture narrative instead of plain cards. |

## Design System Tokens

Core tokens to introduce in `site.css`:
- Color: `--color-ink`, `--color-graphite`, `--color-fog`, `--color-snow`, `--color-azure`, `--color-cobalt-link`, `--color-silver-mist`, `--color-caution`.
- Surfaces: `--surface-canvas`, `--surface-card`, `--surface-recessed`, `--surface-dark`.
- Radius: `--radius-card: 28px`, `--radius-control: 14px`, `--radius-pill: 999px`.
- Spacing: 4px-based scale from 4 to 96.
- Typography: system Apple stack, display/head/body/caption sizes.
- Motion: `--motion-fast: 100ms`, `--motion-standard: 320ms`.

## Component Contracts

Shared shell:
- Light/frosted global nav, not dark Bootstrap nav.
- Active nav is subtle and high-confidence.
- Mobile nav must expose links when expanded and preserve sign-out/change-password access.
- App body max width adapts by route; chat/documents may use wider workspace.

Cards:
- 28px radius for primary panels.
- No heavy box-shadow.
- White surface on fog canvas.
- Internal padding 24-32px.

Buttons:
- Primary: blue pill, one dominant action per panel.
- Secondary: neutral outline or text button.
- Destructive: quiet outline until confirmation.
- Icon buttons: use Bootstrap Icons once the asset is loaded.

Forms:
- Labels stay visible.
- Inputs have 12-14px radius, clear focus ring, and no cramped fixed widths.
- Validation/errors use inline text plus summary where needed.

Tables:
- Desktop tables remain for scanning.
- Mobile switches to stacked rows/cards where tables are too wide.
- Sticky headers only inside intentional scroll regions.

States:
- Loading, empty, error, disabled, permission, and long-running states must be designed.
- Missing Gemini/HuggingFace/FineTune config is not a broken page; it is an explicit setup state.

## File-Level Phases

### Phase 1 - Foundation And Protocol

Files:
- `.state/index.md`
- `.state/tasks/supernova-ui-upgrade/task.md`
- `.state/tasks/supernova-ui-upgrade/repos/CHATBOT.md`
- `docs/supernova-ui-protocol.md`
- `src/PresentationLayer/Pages/Shared/_Layout.cshtml`
- `src/PresentationLayer/wwwroot/css/site.css`

Acceptance:
- Branch and state exist.
- Protocol names libraries, icon/image decisions, phases, and gates.
- Shell uses light Super Nova tokens.
- Build passes.
- Login/chat render without console errors at desktop and mobile.

### Phase 2 - Chat Experience

Files:
- `src/PresentationLayer/Pages/Chat/Index.cshtml`
- `src/PresentationLayer/wwwroot/js/chat.js`
- `src/PresentationLayer/wwwroot/css/site.css`

Work:
- Make conversation/input primary on mobile.
- Move settings to compact panel or off-canvas/mobile accordion.
- Improve message typography, citations, session list, disabled Gemini state, reconnect/error state.
- Replace delete/clear native confirms with Bootstrap modal.

Acceptance:
- `/chat` at 375/768/1280 has no incoherent overflow.
- User can tab through sessions, model options, message input, send, clear.
- Missing Gemini state is clear and polished.

### Phase 3 - Documents And Document Details

Files:
- `src/PresentationLayer/Pages/Documents/Index.cshtml`
- `src/PresentationLayer/Pages/Documents/Details.cshtml`
- `src/PresentationLayer/wwwroot/css/site.css`
- optional `src/PresentationLayer/wwwroot/js/documents.js`

Work:
- Desktop document table becomes polished and compact.
- Mobile document rows become cards.
- Upload panel gains better file affordance and progress state.
- Auto-refresh is replaced or softened to avoid surprising reloads.

Acceptance:
- No page-level horizontal overflow at 375/768/1280.
- Upload disabled/student state is polished.
- Indexing progress, errors, empty state, and actions are readable.

### Phase 4 - Benchmark Dashboard

Files:
- `src/PresentationLayer/Pages/Benchmark/Index.cshtml`
- `src/PresentationLayer/wwwroot/js/benchmark.js`
- `src/PresentationLayer/wwwroot/css/site.css`
- optional Bootstrap Icons local asset under `wwwroot/lib/bootstrap-icons`

Work:
- Fix missing icon library or remove icon classes.
- Replace `prompt()` with Bootstrap modal.
- Add empty chart state panels, chart container sizing, progress treatment.
- Improve metric cards and detailed-results layout.

Acceptance:
- No missing icon rendering.
- Full benchmark run intent is confirmed in a polished modal.
- Empty and data-filled states both work.

### Phase 5 - Admin, Courses, Chapters

Files:
- `src/PresentationLayer/Pages/AdminUsers/Index.cshtml`
- `src/PresentationLayer/Pages/Courses/*.cshtml`
- `src/PresentationLayer/Pages/Chapters/*.cshtml`
- `src/PresentationLayer/wwwroot/css/site.css`

Work:
- Admin actions grouped and risk-aware.
- Remove fixed inline widths.
- Course/chapter forms and tables use the same page-header/form-panel system.

Acceptance:
- Admin table does not clip action controls at desktop.
- Mobile admin actions are usable without horizontal scrolling.
- Destructive actions have clear confirmation affordance.

### Phase 6 - Architecture And Final Polish

Files:
- `src/PresentationLayer/Pages/Architecture/Index.cshtml`
- `src/PresentationLayer/wwwroot/css/site.css`
- docs/QA notes if useful.

Work:
- Convert plain architecture page into visual explanation with workflow lanes.
- Final responsive/keyboard/contrast pass.
- Final push and PR-ready report.

## Verification Protocol

After every commit phase:
- `dotnet build .\Prn222Chatbot.sln`
- Browser route checks on `http://127.0.0.1:5100`
- Desktop: 1280px
- Tablet: 768px
- Mobile: 375px
- Console errors/warnings checked.
- Routes checked according to phase.

Before final completion:
- Anonymous redirect/login.
- Student: chat/documents/details only.
- Teacher: documents/courses/benchmark/architecture.
- Admin: users.
- Chat disabled and ready states if keys allow.
- Documents empty, indexed, processing, failed states where data exists.
- Benchmark empty and progress states.
