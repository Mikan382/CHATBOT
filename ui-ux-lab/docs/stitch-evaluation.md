# Stitch Evaluation - Phase 02

## Durable session

- Project: [PRN222 Course Assistant UI](https://stitch.withgoogle.com/projects/17972450441137138652)
- Project id: `17972450441137138652`
- Created: 2026-07-17
- Stable evidence:
  - `stitch-evidence/phase-02-canvas-overview.png`
  - `stitch-evidence/phase-02-chat-desktop.png`
  - `stitch-evidence/phase-02-chat-tablet.png`
  - `stitch-evidence/phase-02-chat-mobile.png`
  - `stitch-evidence/phase-02-refined-overview.png`

## Repo-grounded prompt contract

The prompt specified an existing ASP.NET Core MVC course assistant, a student question-and-citation job, desktop/tablet/mobile chat frames, the real session/course/connection/quota contracts, long answer and citation states, and an isolated React implementation boundary. It explicitly rejected generic dashboards, invented analytics, glass effects, marketing sections, and backend changes.

## First-pass finding

The Academic Editorial system and desktop anatomy were useful. The first mobile frame failed the chat-first contract by leaving the navigation rail dominant. It also invented attachments, notifications, snippets, PDF summarization, practice quizzes, and media cards.

The same Stitch project was refined rather than replaced. The refinement removed unsupported actions and generated conversation-first mobile, dismissible tablet sessions, a mobile session drawer, and quota/disconnected states.

## Design-system ingestion

- Primary: `#2E5BFF` cobalt
- Secondary: `#1A1F2B` deep ink
- Canvas: `#FAF9F6` warm off-white
- Neutral: `#64748B`
- Headings: Source Serif 4
- UI/body: Inter
- Code/labels: JetBrains Mono
- Direction: editorial academic workspace, restrained borders and shadow, explicit focus, readable long-form responses

## Feasibility matrix

| Variation / idea | Repo mapping | Likely lab files | Contract impact | Responsive evidence | State coverage | Long-content | Classification | Notes |
|---|---|---|---|---|---|---|---|---|
| Academic Editorial tokens | CSS custom properties and type roles | `src/styles/` | None | Complete | Partial | Complete | Apply Now | Maps to ordinary CSS without changing the current app |
| Desktop global rail + session rail + conversation | Chat shell and route composition | `src/features/chat/` | Low | Complete | Partial | Complete | Apply Now | Preserve explicit scroll owners |
| Refined tablet dismissible session drawer | responsive shell state | `src/components/shell/`, chat state | Low | Complete | Partial | Complete | Apply Now | Conversation remains primary |
| Refined mobile conversation-first chat | mobile shell and composer | chat components and CSS | Low | Complete | Complete | Complete | Apply Now | Navigation and sessions stay in drawers |
| Mobile session drawer | session list component | chat session components | Low | Complete | Complete | Partial | Apply Now | Existing list/search/rename/delete APIs support it |
| Quota and disconnected treatment | status banner and disabled composer | chat status components | Low | Complete | Complete | Complete | Apply Now | Subscription action maps to the existing subscription route |
| Attachments, notifications, snippets, media cards | no matching contract | none | High | Partial | Partial | Partial | Reject | Invented by first generation and removed from implementation scope |
| Generated HTML/code export | supporting evidence only | none | Unknown | Unknown | Unknown | Unknown | Reject | Production code will be component-first and repo-owned |

## Decision

The refined direction passes the implementation gate. It maps to the existing API and SignalR concepts, uses a narrow component system, defines desktop/tablet/mobile behavior, handles long answers, sessions, quota, and disconnection, and requires no backend or database change.

Implementation must not reproduce Stitch pixels literally. Build and verify isolated components first, then compose screens.
