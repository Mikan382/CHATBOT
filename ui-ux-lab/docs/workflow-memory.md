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

### Separate UI lab must still match the repository presentation stack
- Symptom: an isolated design folder was initially scaffolded as React/Vite even though the current product renders MVC Razor Views.
- Cause: "independent UI" was interpreted as a separate frontend ecosystem instead of a separately runnable project using the same presentation model.
- Durable fix: use an independent ASP.NET Core MVC project with Razor Views, CSS, and browser JavaScript; keep it out of the existing solution and runtime path.
- Verification: the lab project builds on its own, the existing solution build remains unchanged, and the React/Vite scaffold never reaches a commit.
- Reuse when: the user asks for an isolated reference implementation inside a server-rendered .NET repository.

### Optional live proxy must fail back quickly
- Symptom: `?mode=live` remained on “Checking live app…” when the existing application was offline, making the standalone lab look stalled.
- Cause: the reverse proxy and browser fetch could wait much longer than a design-review interaction should.
- Durable fix: retain fixture as the default, bound the YARP request activity to five seconds, and abort the browser probe after 3.5 seconds before labeling the fixture fallback.
- Verification: with port 5096 offline, the top-bar status changes from “Checking live app…” to “Fixture fallback” and the fixture sessions remain available.
- Reuse when: an optional integration preview depends on a local service that reviewers may not be running.

### Windows build output is locked while the lab is running
- Symptom: the final lab build reports `MSB3027`/`MSB3021` because `Prn222.UiLab.exe` cannot be replaced.
- Cause: the browser-QA server is still running from the same Debug output directory.
- Durable fix: resolve and stop only the `Prn222.UiLab` process whose executable path is inside this repo, run the build gate, then restart the hidden review server after delivery.
- Verification: the repeated standalone lab build completes with 0 warnings and 0 errors.
- Reuse when: Windows reports an apphost or executable lock immediately after local UI testing.

### Real database mode must stay behind the application boundary
- Symptom: a request to “use the real DB” can tempt an isolated UI project to copy the production connection string or add its own EF Core context.
- Cause: direct database access looks faster than defining a narrow UI-facing contract, but it duplicates authorization and business rules.
- Durable fix: add authenticated read projections to the existing presentation layer, call existing services, forward them through the lab's same-origin proxy, and keep fixture content visibly labeled when live mode is unavailable.
- Verification: the API controller contains `[Authorize]`, role-restricted endpoints retain Admin boundaries, the lab contains no `AppDbContext` or connection string, and live pages replace fixture rows only after an authenticated response.
- Reuse when: a prototype or reference UI needs current data from an established server application.

### Proxied Razor login forms post to their generated root path
- Symptom: the login page loads through `/backend/Account/Login`, but submitting it stays unauthenticated because the Razor form action is `/Account/Login`.
- Cause: YARP removes `/backend` before the existing MVC app renders HTML, so Tag Helpers correctly generate the existing app's root-relative action without the lab prefix.
- Durable fix: proxy the narrow `/Account/{**catch-all}` route to the same application cluster in addition to `/backend/{**catch-all}`; keep all other existing-app routes behind `/backend`.
- Verification: GET through `/backend/Account/Login` supplies the antiforgery cookie, POST to `/Account/Login` reaches the real AccountController, and its local return URL lands back on the lab live page.
- Reuse when: server-rendered forms are displayed through a prefixed reverse proxy but emit root-relative form actions.
