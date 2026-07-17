# Repo State: CHATBOT

## Scope
- Workspace: chatbot-course
- Task: ui-ux-lab-live-data
- Repo: CHATBOT
- Allowed paths / out of scope: UI lab, one authenticated read API controller, and task state only; no direct DB access from the lab and no database schema changes.

## Branch / remote
- Claimed branch: feature/ui-ux-lab-live-data
- Claimed base: origin/main at 2bb46ff
- Remote: `origin/feature/ui-ux-lab-live-data`
- Current checkout verified: feature/ui-ux-lab-live-data via git status on 2026-07-17
- Remote verified: origin/main fetched at 2bb46ff on 2026-07-17
- Ahead/behind verified: branch created directly from origin/main on 2026-07-17

## Delivery state
- Pushed: yes, implementation commit `5dffd0d`
- PR: https://github.com/Mikan382/CHATBOT/pull/18 (merged)
- Gate: passed locally
- Gate evidence: `dotnet build Prn222Chatbot.sln --nologo`; `dotnet build ui-ux-lab/Prn222.UiLab.csproj --nologo`; Node syntax checks; `ui-ux-lab/tools/verify-adapter.ps1`; `git diff --check` on 2026-07-17.

## Repo progress
- Done: live-data contract and role boundaries traced through existing services and repositories.
- Done: authenticated API projections and live DOM bindings implemented without adding a DbContext or connection string to the isolated lab.
- Done: real app connected to LocalDB; unauthenticated proxy API returned 401 as designed and the proxied login POST reached the existing AccountController.
- Done: PR #18 merged as `1618f4d`; `origin/main` contains `5005d26` and the live-data implementation.
- Next repo step: none for this task.
- Blocked on: none

## Shared path ownership
- ui-ux-lab/ -> CHATBOT
- src/PresentationLayer/Controllers/UiLabApiController.cs -> CHATBOT
