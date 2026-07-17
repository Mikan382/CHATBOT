# CHATBOT UI/UX Lab

This folder is an independent design-reference application for the PRN222 RAG Chatbot.

## Isolation contract

- It is not added to `Prn222Chatbot.sln`.
- It does not replace or modify any Razor Page under `src/`.
- It does not change backend, database, authentication, API, or SignalR contracts.
- It may call the existing application over HTTP when live mode is enabled.
- It must retain fixture-backed preview mode so reviewers can inspect the design without local services or secrets.
- Merging this folder into `main` must not change the current build or runtime path.

Implementation starts only after the Stitch evaluation chooses a repo-compatible direction.

See [docs/roadmap.md](docs/roadmap.md) for the 20-phase delivery plan.
