# PRN222 Course Assistant

An ASP.NET Core MVC application for managing PRN222 course materials and answering
student questions with retrieval-augmented generation (RAG). Built as a three-layer
(Presentation / Business / Data Access) solution on .NET 8 and SQL Server.

> Internal / educational project. Payments run against the **VNPay sandbox** (no real money) and it is not intended for production deployment.

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Demo Accounts](#demo-accounts)
- [Roles & Routes](#roles--routes)
- [Project Structure](#project-structure)
- [Database & Migrations](#database--migrations)
- [Scope](#scope)

## Features

- **Authentication & roles** — cookie login with `Student`, `Teacher`, and `Admin` roles.
- **Course management** — course/chapter CRUD and teacher-to-course assignment.
- **Document indexing** — synchronous text extraction, duplicate detection, and chunking. The chunking strategy used for each document is recorded and shown in the UI.
- **RAG chat** — SignalR-based chat with citations and searchable, renameable, clearable session history.
- **Configurable chunking** — admin-selectable global strategy: `paragraph`, configurable `fixed`, or `sentence`.
- **RAG benchmark** — compare chunking strategies and Hugging Face embedding models against a curated ground-truth set; toggle questions active/inactive to control which run.
- **Subscriptions & payment** — students buy packages through the **VNPay sandbox**; a paid package auto-activates on a verified payment callback (no admin approval step). Each package sets a **monthly chat-message quota** enforced in chat. Payment confirmation is idempotent (return + IPN), and the price is snapshotted at activation so later price edits do not rewrite existing subscriptions. Admins manage packages, revoke active subscriptions, and see an estimated monthly value.

## Tech Stack

| Area | Technology |
|---|---|
| Runtime | .NET 8 (ASP.NET Core MVC) |
| Real-time | SignalR |
| Data | Entity Framework Core 8, SQL Server / LocalDB |
| Frontend | Razor Views, Bootstrap 5, vanilla JS |
| AI | Google Gemini (chat), Hugging Face (embeddings) |

## Architecture

```text
User
  -> PresentationLayer   (MVC Controllers, Razor Views, SignalR Hub, ViewModels)
  -> BusinessLayer       (services, DTOs, AI clients, parsing / indexing / retrieval)
  -> DataAccessLayer     (repositories, EF Core, AppDbContext, SQL Server)
```

- `Program.cs` is the composition root: it wires Business/Data Access implementations and initializes the database. All other Presentation code depends on Business **interfaces and DTOs**, never on EF Core entities.
- Interfaces live at layer boundaries (repositories, external AI clients, replaceable chunking strategies). One-off internal orchestrators stay concrete.
- External AI calls go through client abstractions. Document upload/indexing is synchronous inside the Business layer (no background worker or queue).

## Prerequisites

- The .NET SDK pinned by [`global.json`](global.json)
- SQL Server LocalDB (or any SQL Server instance)
- A Google Gemini API key (required for chat replies)
- A Hugging Face API key (optional for chat; **required** to run a benchmark)

## Getting Started

### 1. Clone and restore

```powershell
git clone <repository-url>
cd CHATBOT
dotnet restore .\Prn222Chatbot.sln
```

### 2. Configure secrets

Keep secrets out of tracked configuration with User Secrets:

```powershell
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_KEY" --project .\src\PresentationLayer
dotnet user-secrets set "HuggingFace:ApiKey" "YOUR_HUGGINGFACE_KEY" --project .\src\PresentationLayer
dotnet user-secrets set "SeedUsers:Admin:Password" "YOUR_ADMIN_PASSWORD" --project .\src\PresentationLayer
dotnet user-secrets set "VnPay:TmnCode" "YOUR_VNPAY_TMNCODE" --project .\src\PresentationLayer
dotnet user-secrets set "VnPay:HashSecret" "YOUR_VNPAY_HASHSECRET" --project .\src\PresentationLayer
```

Register a sandbox merchant at <https://sandbox.vnpayment.vn/devreguser/> to obtain `TmnCode` and `HashSecret`. Without them, chat and the rest of the app still work; only the **Buy with VNPay** action is disabled.

### 3. Run

```powershell
dotnet build .\Prn222Chatbot.sln --no-restore
dotnet run --project .\src\PresentationLayer
```

Open <http://localhost:5096> (default `http` profile).

On startup the app applies pending EF Core migrations and seeds required settings. Demo data is inserted **only when the database is empty**, so later seed-configuration changes never overwrite stored data.

## Configuration

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string (override in `appsettings.json`, User Secrets, or the `ConnectionStrings__DefaultConnection` env var) |
| `Gemini:ApiKey` | Google Gemini key used for chat generation |
| `HuggingFace:ApiKey` | Hugging Face key used for embeddings and benchmarks |
| `HuggingFace:Models` | Benchmark embedding models — each needs its model URL and any query/passage prefixes |
| `SeedUsers:{Role}:Password` | Optional seed password per demo role |
| `VnPay:TmnCode` / `VnPay:HashSecret` | VNPay sandbox merchant code and HMAC secret (keep in User Secrets) |
| `VnPay:ReturnUrl` | Browser return URL after payment (default `http://localhost:5096/Payment/VnpayReturn`) |

Retrieval falls back to lexical search when Hugging Face is not configured, so normal chat works with Gemini alone. Benchmarks re-embed a temporary corpus on every run, so provider usage limits apply. The default benchmark compares `intfloat/multilingual-e5-base` and `intfloat/multilingual-e5-small`.

## Demo Accounts

Fresh databases seed the PRN222 course (`Course Introduction` at order `0` and Chapters 01–08) and the accounts below. When no seed password secret is set, the default password is `Prn222@123`.

| Role | Email |
|---|---|
| Student | `student@prn222.local` |
| Teacher | `teacher@prn222.local` |
| Admin | `admin@prn222.local` |

## Roles & Routes

| Route | Access | Purpose |
|---|---|---|
| `/chat` | All roles | RAG chat and session history (quota-limited for students) |
| `/documents` | All roles | Browse/filter; Teacher/Admin can upload and delete |
| `/courses` | Teacher/Admin | Assigned courses or full management |
| `/AdminUsers` | Admin | User CRUD, lockout, roles, and password reset |
| `/settings` | Admin | Global chunking strategy |
| `/benchmark` | Admin | Run experiments and compare RAG metrics |
| `/benchmark/questions` | Admin | Ground-truth question CRUD and active/inactive toggle |
| `/subscriptions` | Student | Browse packages and buy one via VNPay |
| `/payment` | Student / callback | `Checkout` starts a VNPay payment; `VnpayReturn` (browser) and `VnpayIpn` (server) confirm it |
| `/subscriptions/dashboard` | Admin | Manage packages, revoke subscriptions, view statistics |

## Project Structure

```text
src/
  PresentationLayer/    Controllers/  Hubs/  ViewModels/  Views/  wwwroot/
  BusinessLayer/        Services/  DTOs/  AI/  Parsing/  Indexing/  Retrieval/
  DataAccessLayer/      Entities/  Enums/  Repositories/  Data/
assets/
  prn222-ground-truth.json   # 50 curated benchmark questions (reference data, not auto-imported)
```

## Database & Migrations

Startup applies pending migrations automatically. To run them without launching the web app:

```powershell
dotnet ef database update --project .\src\DataAccessLayer --startup-project .\src\DataAccessLayer --context AppDbContext
```

`AppDbContextFactory` provides the design-time context for this command. Migration files are append-only history; older migration names do not necessarily reflect current runtime behavior.

Notes:

- Embeddings are stored as JSON in SQL Server (assignment/demo scope).
- Benchmark runs re-chunk `Document.ContentText` in memory and embed both corpus chunks and questions with the selected model. Only ground truth, completed runs, and result metrics are persisted; production chat indexes are untouched.
- The versioned document-hash bootstrap keeps the earliest document when a chapter contains duplicate normalized content.

## Scope

Deployment, background workers, and fine-tuned models are out of scope. Payment is integrated against the **VNPay sandbox** — the flow, signing, and idempotent confirmation are real, but no real money moves and it does not represent production billing. Subscriptions gate chat usage through message quotas.
