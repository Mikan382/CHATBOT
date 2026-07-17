# Phase 20 Verification

Verified on 2026-07-17 against `origin/main` at `1f39a6a`.

## Automated gates

- `dotnet build ui-ux-lab/Prn222.UiLab.csproj --nologo`: 0 warnings, 0 errors.
- `dotnet build Prn222Chatbot.sln --nologo`: current application build remains green.
- `ui-ux-lab/tools/verify-adapter.ps1`: fixture default, REST path, SignalR path, proxy transform, and fallback contract verified.
- `git diff origin/main --name-only`: only `ui-ux-lab/` and the task-specific `.state/` handoff files may differ.

## Browser matrix

The reference routes for Chat, Documents, Document Details, Courses, Benchmark, Admin, and Architecture were exercised in the in-app browser.

| Viewport | Coverage | Result |
|---|---|---|
| 390 × 844 | All seven reference routes | No viewport or main-region horizontal overflow; mobile navigation visible |
| 1440 × 900 | All seven reference routes | Correct page heading, active rail item, fixture label, and no horizontal overflow |
| 1698 × 759 | Chat and Architecture | No viewport or main-region horizontal overflow |
| 1920 × 1080 | Chat and Architecture | No viewport or main-region horizontal overflow |

Additional interaction checks:

- Mobile session drawer opens and updates `aria-expanded`.
- Course archive action opens a labeled confirmation dialog.
- Live mode falls back to fixtures when the current application is offline.
- Each reference route has exactly one `h1`, no duplicate IDs, and no visibly unnamed interactive controls in the lightweight DOM audit.
- No browser console errors or warnings were observed in the final responsive pass.

## Merge boundary

The lab is a standalone ASP.NET Core MVC project using Razor Views, Bootstrap, CSS, and browser JavaScript. It is not referenced by `Prn222Chatbot.sln`; merging it does not change the current startup project, application routes, backend contracts, database, or deployment path.
