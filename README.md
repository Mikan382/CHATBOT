# PRN222 RAG Chatbot

ASP.NET Core MVC application for a role-based RAG chatbot used in the PRN222 assignment. The current scope is demo/internal: course and chapter management, teacher-course assignment, document upload with synchronous indexing, RAG chat with citations, and admin-managed chunking strategy.

## Project Status

| Item | Value |
|---|---|
| Runtime | ASP.NET Core MVC Controllers + Razor Views |
| Target framework | `net8.0` |
| Database | SQL Server LocalDB by default |
| ORM | EF Core |
| Authentication | Custom cookie auth with password hashing |
| Roles | `Student`, `Teacher`, `Admin` |
| Architecture | `PresentationLayer -> BusinessLayer -> DataAccessLayer -> SQL Server` |

## Features

- Login/logout, change password, and admin user management.
- Role-based access control for Student, Teacher, and Admin.
- Admin assigns multiple teachers to each course.
- Teachers manage only assigned courses, chapters, and documents.
- Upload `.pdf`, `.docx`, `.pptx`, `.txt`, and `.md` materials.
- Upload runs synchronously: extract text, check duplicate content, chunk, embed when configured, then save.
- Duplicate documents are blocked per chapter by `ContentHash`.
- RAG chat through SignalR with SQL Server chat history and citations.
- Hugging Face embedding retrieval when configured; lexical fallback otherwise.
- Gemini-based answer generation.
- Admin page for global chunking strategy: `paragraph`, `fixed_1000`, `sentence`.

Removed from current scope:

- ASP.NET Identity runtime/schema.
- Background worker/indexing queue/progress columns.
- Fine-tuned model modes/endpoints.
- Benchmark/evaluation dashboard and tables.

## Roles and Permissions

| Feature | Student | Teacher | Admin |
|---|---:|---:|---:|
| Chat by selected course | Yes | Yes | Yes |
| View documents | Yes | Yes | Yes |
| Upload/delete documents | No | Assigned courses | Yes |
| Manage chapters | No | Assigned courses | Yes |
| Manage courses/teacher assignment | No | No | Yes |
| User management | No | No | Yes |
| Chunking settings | No | No | Yes |

## Solution Layout

```text
src/
  PresentationLayer/
    Controllers/        MVC controllers for rendered pages and form posts
    ApiControllers/     JSON API endpoints
    Hubs/               SignalR hub
    Views/              Razor Views
    ViewModels/         View and input models

  BusinessLayer/
    Services/           Auth, chat, document, course, chapter, user, settings
    AI/                 Gemini and Hugging Face embedding clients
    Indexing/           Chunking and synchronous indexing orchestration
    Parsing/            PDF, DOCX, PPTX, TXT, and MD extraction
    Retrieval/          Embedding retrieval and lexical fallback
    DTOs/               Data transfer records

  DataAccessLayer/
    Entities/           EF Core entities and enums
    Repositories/       Data access boundary
    Data/               AppDbContext, migrations, seed/bootstrapper
```

## Configuration

Set local secrets with User Secrets:

```powershell
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_KEY" --project .\src\PresentationLayer
dotnet user-secrets set "HuggingFace:ApiKey" "YOUR_HUGGINGFACE_KEY" --project .\src\PresentationLayer
dotnet user-secrets set "SeedUsers:Admin:Password" "CHANGE_ME_ADMIN_PASSWORD" --project .\src\PresentationLayer
```

Default seeded accounts use `Prn222@123` when no password secret is configured:

| Role | Email |
|---|---|
| Student | `student@prn222.local` |
| Teacher | `teacher@prn222.local` |
| Admin | `admin@prn222.local` |

## Run Locally

```powershell
dotnet restore .\Prn222Chatbot.sln
dotnet build .\Prn222Chatbot.sln
dotnet ef database update --project .\src\DataAccessLayer --startup-project .\src\DataAccessLayer --context AppDbContext
dotnet run --project .\src\PresentationLayer --urls http://127.0.0.1:5100
```

## Main Pages

| Route | Access | Purpose |
|---|---|---|
| `/account/login` | Anonymous | Login page |
| `/chat` | Student, Teacher, Admin | Realtime RAG chat |
| `/documents` | Student, Teacher, Admin | List, filter, upload, delete, inspect documents |
| `/courses` | Teacher, Admin | Assigned course list for Teacher; full management for Admin |
| `/admin/users` | Admin | User management |
| `/settings` | Admin | Global chunking strategy |
| `/architecture` | Teacher, Admin | Architecture explanation |

## Verification

```powershell
dotnet build .\Prn222Chatbot.sln --no-restore
dotnet ef database update --project .\src\DataAccessLayer --startup-project .\src\DataAccessLayer --context AppDbContext
rg "FineTune|Benchmark|BackgroundIndexing|IIndexingQueue|DocumentIndexStatus" src\BusinessLayer src\PresentationLayer src\DataAccessLayer\Entities src\DataAccessLayer\Repositories -g "*.cs" -g "*.cshtml" -g "*.js"
```

Expected:

- Build has `0 Warning(s)` and `0 Error(s)`.
- Runtime code has no fine-tune, benchmark, background worker, or indexing progress symbols.
- `PresentationLayer` references only `BusinessLayer`.
- `BusinessLayer` references `DataAccessLayer`.
- `DataAccessLayer` has no project references.
