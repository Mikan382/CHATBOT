# PRN222 Course Assistant

Internal/demo ASP.NET Core MVC application for managing PRN222 course materials and answering questions with retrieval-augmented generation (RAG).

## Scope

- Cookie login with `Student`, `Teacher`, and `Admin` roles.
- Course/chapter CRUD and teacher-course assignment.
- Synchronous document extraction, duplicate detection, chunking, and optional embeddings.
- SignalR chat with citations and searchable, renameable, clearable session history.
- Admin-managed chunking strategy: `paragraph`, `fixed_1000`, or `sentence`.
- RAG benchmark for comparing chunking strategies and Hugging Face embedding models against curated ground truth.
- Internal subscription registration, package management, and dashboard statistics.

Payment, subscription entitlements, deployment, background workers, and fine-tuned models are outside this assignment.

## Architecture

```text
User
  -> PresentationLayer (MVC Controllers, Razor Views, SignalR Hub)
  -> BusinessLayer     (services, DTOs, AI clients, parsing/indexing/retrieval)
  -> DataAccessLayer   (repositories, EF Core, SQL Server)
```

`Program.cs` is the composition root. It references Business/DataAccess implementations for dependency registration and database initialization. Other Presentation code depends on Business interfaces and DTOs, not EF Core entities.

Interfaces are kept at layer boundaries, repositories, external clients, and replaceable chunking strategies. One-off internal orchestrators remain concrete. See [ARCHITECTURE_AUDIT.md](ARCHITECTURE_AUDIT.md) for the boundary checklist.

## Run Locally

Requirements: the .NET SDK selected by [`global.json`](global.json), SQL Server LocalDB (or another SQL Server), and internet access for configured AI providers and the SignalR browser-client CDN.

Store secrets outside tracked configuration:

```powershell
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_KEY" --project .\src\PresentationLayer
dotnet user-secrets set "HuggingFace:ApiKey" "YOUR_HUGGINGFACE_KEY" --project .\src\PresentationLayer
dotnet user-secrets set "SeedUsers:Admin:Password" "YOUR_ADMIN_PASSWORD" --project .\src\PresentationLayer
```

Gemini is required for chat replies. Hugging Face is optional for normal chat because retrieval falls back to lexical search. Both API keys are required to run a benchmark.

Benchmark embedding models are configured in `HuggingFace:Models`. Each model must provide its model URL and any query/passage prefixes required by that model.
The default comparison uses `intfloat/multilingual-e5-base` and `intfloat/multilingual-e5-small`. Provider usage limits still apply because each benchmark re-embeds its temporary corpus.

```powershell
dotnet restore .\Prn222Chatbot.sln
dotnet build .\Prn222Chatbot.sln --no-restore
dotnet run --project .\src\PresentationLayer
```

Open `http://localhost:5096` for the default `http` profile. Override the LocalDB connection in `appsettings.json` with User Secrets or `ConnectionStrings__DefaultConnection` when needed.

Startup applies pending migrations and required settings. Demo data is inserted only when the database is empty, so later seed configuration changes do not overwrite stored data.

## Demo Accounts

Fresh databases use `Prn222@123` when no seed password secret is configured.
They also seed the PRN222 course with `Course Introduction` at order `0` and Chapters 01-08 at their matching orders.

| Role | Email |
|---|---|
| Student | `student@prn222.local` |
| Teacher | `teacher@prn222.local` |
| Admin | `admin@prn222.local` |

## Main Routes

| Route | Access | Purpose |
|---|---|---|
| `/chat` | All roles | RAG chat and session history |
| `/documents` | All roles | Browse/filter; Teacher/Admin can upload and delete |
| `/courses` | Teacher/Admin | Assigned courses or full management |
| `/AdminUsers` | Admin | User CRUD, lockout, roles, and password reset |
| `/settings` | Admin | Global chunking strategy |
| `/benchmark` | Admin | Run experiments and compare RAG metrics |
| `/benchmark/questions` | Admin | Ground-truth question CRUD |
| `/subscriptions` | Student | Register, switch, or cancel a demo package |
| `/subscriptions/dashboard` | Admin | Packages and registration statistics |
| `/architecture` | Teacher/Admin | Current architecture overview |

## Project Layout

```text
src/
  PresentationLayer/
    Controllers/  Hubs/  ViewModels/  Views/  wwwroot/
  BusinessLayer/
    Services/  DTOs/  AI/  Parsing/  Indexing/  Retrieval/
  DataAccessLayer/
    Entities/  Enums/  Repositories/  Data/
```

## Database Notes

Run migrations without starting the web app:

```powershell
dotnet ef database update --project .\src\DataAccessLayer --startup-project .\src\DataAccessLayer --context AppDbContext
```

`AppDbContextFactory` supports this design-time command. Migration files are append-only history; legacy migration names do not indicate current runtime features.

The versioned document-hash bootstrap keeps the earliest document if old data contains duplicate normalized content within one chapter.

Benchmark runs re-chunk `Document.ContentText` in memory and embed both corpus chunks and questions with the selected model. Only ground truth, completed runs, and result metrics are persisted; production chat indexes are not modified.

The portable demo ground-truth set is stored in [`assets/prn222-ground-truth.json`](assets/prn222-ground-truth.json). It contains 50 curated questions: two for the course introduction and six for each of Chapters 01-08. Each item names its expected PPTX source; the JSON is reference data and is not imported automatically at startup.
