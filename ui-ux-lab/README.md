# CHATBOT UI/UX Lab

This folder is an independent ASP.NET Core MVC design-reference application for the PRN222 RAG Chatbot.

## Isolation contract

- It is not added to `Prn222Chatbot.sln`.
- It uses the same presentation approach as the current application: MVC controllers, Razor Views, CSS, and browser JavaScript.
- It does not replace or modify any Razor Page under `src/`.
- It does not change backend, database, authentication, API, or SignalR contracts.
- It may call the existing application over HTTP when live mode is enabled.
- It must retain fixture-backed preview mode so reviewers can inspect the design without local services or secrets.
- Merging this folder into `main` must not change the current build or runtime path.

Implementation starts only after the Stitch evaluation chooses a repo-compatible direction.

## Run the lab

```powershell
dotnet run --project .\ui-ux-lab\Prn222.UiLab.csproj
```

Open `http://127.0.0.1:5177`.

## Review routes

| Reference | URL |
|---|---|
| Sign-in states | `/Reference/Login?state=default` |
| Chat, sessions, citations, composer | `/Reference/Chat` |
| Document library and detail | `/Reference/Documents` and `/Reference/DocumentDetails` |
| Courses and chapters | `/Reference/Courses` |
| Benchmark dashboard | `/Reference/Benchmark?state=running` |
| User administration | `/Reference/Admin` |
| Architecture and integration boundary | `/Reference/Architecture` |

Fixture mode is always the default. Start the existing application separately on `http://localhost:5096`, sign in there, then add `?mode=live` to a lab reference URL to probe the existing REST and SignalR contracts through `/backend`. A failed live probe returns to fixtures within a few seconds and labels that state in the top bar.

Run the adapter contract gate with:

```powershell
.\ui-ux-lab\tools\verify-adapter.ps1
```

See [docs/roadmap.md](docs/roadmap.md) for the 20-phase delivery plan.
