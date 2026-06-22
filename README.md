# PRN222 RAG Chatbot

ASP.NET Core MVC application for a role-based RAG chatbot used in the PRN222 assignment. The system manages courses, chapters, uploaded learning materials, background indexing, realtime chat with citations, and benchmark results for RAG versus a custom fine-tuned endpoint.

## Project Status

| Item | Value |
|---|---|
| Runtime | ASP.NET Core MVC / Razor Views |
| Target framework | `net8.0` |
| SDK | `global.json` pins .NET SDK `9.0.304` with roll-forward |
| Database | SQL Server LocalDB by default |
| ORM | EF Core |
| Authentication | ASP.NET Identity |
| Roles | `Student`, `Teacher`, `Admin` |
| Architecture | `PresentationLayer -> BusinessLayer -> DataAccessLayer -> SQL Server` |
| Secrets | User Secrets or environment variables |
| `.env` | Not used |
| UI language | English |

## Features

- Login/logout with ASP.NET Identity.
- Role-based access control for Student, Teacher, and Admin.
- Course and chapter management.
- Upload `.pdf`, `.docx`, `.pptx`, `.txt`, and `.md` materials.
- Server-side upload validation.
- Text extraction for PDF, DOCX, PPTX, TXT, and MD.
- Background document indexing with visible progress percentage and stage.
- Document details page with extracted text, chunks, embedding status, and indexing errors.
- RAG chat through SignalR with SQL Server chat history.
- Course selector for multi-course RAG retrieval.
- Hugging Face embedding retrieval when configured.
- Lexical retrieval fallback when embeddings are unavailable.
- Gemini-based RAG answer generation with citations.
- Fine-tuned model mode through a real custom REST endpoint.
- Benchmark dashboard for RAG/fine-tuned comparison with Chart.js charts.
- Multi-chunking strategy benchmark (paragraph, fixed-size, sentence-based).
- Multi-embedding model benchmark (multilingual-e5-base, text-embedding-3-small, PhoBERT-base, bge-m3).
- LLM-based RAGAS scoring (Faithfulness, Answer Relevance, Context Recall, Citation Accuracy).
- Full comparative benchmark across all strategy × model combinations.
- Benchmark results export as JSON.
- Vietnamese language support in RAG chat responses.
- Architecture page explaining MVC, 3-Layers, SignalR, Worker Service, and EF Core flow.

## Roles and Permissions

| Feature | Student | Teacher | Admin |
|---|---:|---:|---:|
| Login / Logout | Yes | Yes | Yes |
| Chat by selected course | Yes | Yes | Yes |
| View documents | Yes | Yes | Yes |
| View document details/chunks | Yes | Yes | Yes |
| Upload documents | No | Yes | Yes |
| Delete documents | No | Yes | Yes |
| CRUD courses | No | Yes | Yes |
| CRUD chapters | No | Yes | Yes |
| Benchmark dashboard | No | Yes | Yes |
| Run benchmark | No | Yes | Yes |
| Architecture page | No | Yes | Yes |
| User management | No | No | Yes |

## Tech Stack

| Area | Technology |
|---|---|
| Web UI | ASP.NET Core MVC, Razor Views, Bootstrap |
| Authentication | ASP.NET Identity |
| Realtime chat | SignalR |
| Database | SQL Server LocalDB / SQL Server |
| ORM | EF Core |
| Background jobs | HostedService / BackgroundService |
| PDF parsing | UglyToad.PdfPig |
| DOCX/PPTX parsing | DocumentFormat.OpenXml |
| AI generation | Gemini REST API |
| Embeddings | Hugging Face Inference API |
| Fine-tuned model | Custom REST endpoint |

## Solution Layout

```text
Prn222Chatbot.sln
src/
  PresentationLayer/
    Controllers/      MVC controllers and API endpoints
    Hubs/             SignalR hub
    ViewModels/       View and input models
    Views/            Razor views
    wwwroot/          CSS, JavaScript, client assets

  BusinessLayer/
    Services/         Auth, chat, document, course, chapter, user, benchmark orchestration
    AI/               Gemini, Hugging Face embedding, and fine-tuned clients
    Indexing/         Background worker, indexing queue, chunking workflow
    Parsing/          PDF, DOCX, PPTX, TXT, and MD extraction
    Retrieval/        Embedding retrieval and lexical fallback
    DTOs/             Data transfer records

  DataAccessLayer/
    Entities/         EF Core entities and enums
    Repositories/     Data access boundary
    Data/             AppDbContext, migrations, seed/bootstrapper
```

## Architecture Rules

The project follows a strict 3-layer structure:

| Layer | Responsibility |
|---|---|
| `PresentationLayer` | Razor Views, Controllers, SignalR Hub, ViewModels, browser assets |
| `BusinessLayer` | Services, validation, orchestration, AI/RAG flow, parsing, indexing, scoring |
| `DataAccessLayer` | Repositories, EF Core entities, `AppDbContext`, migrations, SQL Server access |

Project references:

| Project | References |
|---|---|
| `DataAccessLayer` | none |
| `BusinessLayer` | `DataAccessLayer` |
| `PresentationLayer` | `BusinessLayer`, `DataAccessLayer` |

Rules:

- Controllers and Hubs call Services only.
- Services handle validation, orchestration, AI/RAG flow, and DTO mapping.
- Repositories are the only layer that queries or updates `AppDbContext`.
- Razor Views do not inject or query `AppDbContext`.
- `AppDbContext` is registered as scoped, not singleton.
- Secrets are not stored in committed configuration files.

## Configuration

`src/PresentationLayer/appsettings.json` is committed and contains only safe defaults:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=Prn222RagChatbot;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Gemini": {
    "Model": "gemini-2.5-flash"
  },
  "HuggingFace": {
    "ModelName": "intfloat/multilingual-e5-base",
    "ModelUrl": "https://router.huggingface.co/hf-inference/models/intfloat/multilingual-e5-base/pipeline/feature-extraction"
  },
  "FineTune": {
    "EndpointUrl": ""
  }
}
```

Do not commit real API keys or passwords.

Set local secrets with User Secrets:

```powershell
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_KEY" --project .\src\PresentationLayer
dotnet user-secrets set "HuggingFace:ApiKey" "YOUR_HUGGINGFACE_KEY" --project .\src\PresentationLayer
dotnet user-secrets set "FineTune:EndpointUrl" "https://your-endpoint.example/chat" --project .\src\PresentationLayer
dotnet user-secrets set "FineTune:ApiKey" "YOUR_FINE_TUNE_API_KEY" --project .\src\PresentationLayer
```

Seed demo user passwords:

```powershell
dotnet user-secrets set "SeedUsers:Student:Password" "CHANGE_ME_STUDENT_PASSWORD" --project .\src\PresentationLayer
dotnet user-secrets set "SeedUsers:Teacher:Password" "CHANGE_ME_TEACHER_PASSWORD" --project .\src\PresentationLayer
dotnet user-secrets set "SeedUsers:Admin:Password" "CHANGE_ME_ADMIN_PASSWORD" --project .\src\PresentationLayer
```

Default seeded accounts:

| Role | Email |
|---|---|
| Student | `student@prn222.local` |
| Teacher | `teacher@prn222.local` |
| Admin | `admin@prn222.local` |

For deployment or CI, use standard ASP.NET Core environment variables:

```powershell
$env:Gemini__ApiKey="YOUR_GEMINI_KEY"
$env:HuggingFace__ApiKey="YOUR_HUGGINGFACE_KEY"
$env:FineTune__EndpointUrl="https://your-endpoint.example/chat"
$env:FineTune__ApiKey="YOUR_FINE_TUNE_API_KEY"
$env:SeedUsers__Admin__Password="CHANGE_ME_ADMIN_PASSWORD"
```

Notes:

- `ConnectionStrings:DefaultConnection` is required.
- `Gemini:ApiKey` is required for live RAG answer generation.
- `HuggingFace:ApiKey` is required for embedding-based indexing/retrieval.
- If Hugging Face is missing, documents are still chunked and lexical retrieval is used.
- If `FineTune:EndpointUrl` is empty, fine-tuned options are disabled.
- `.env` files are not loaded.

## Run Locally

Restore and build:

```powershell
dotnet restore .\Prn222Chatbot.sln
dotnet build .\Prn222Chatbot.sln
```

Apply migrations:

```powershell
dotnet ef database update --project .\src\DataAccessLayer --startup-project .\src\PresentationLayer
```

Run:

```powershell
dotnet run --project .\src\PresentationLayer --urls http://127.0.0.1:5100
```

Open:

```text
http://127.0.0.1:5100
```

## Main Pages

| Route | Access | Purpose |
|---|---|---|
| `/account/login` | Anonymous | Login page |
| `/chat` | Student, Teacher, Admin | Realtime RAG chat |
| `/documents` | Student, Teacher, Admin | List, filter, and inspect documents |
| `/documents/{id}` | Student, Teacher, Admin | View extracted text, chunks, embeddings, progress |
| `/courses` | Teacher, Admin | Course CRUD |
| `/courses/create` | Teacher, Admin | Create course |
| `/courses/{id}/edit` | Teacher, Admin | Edit course |
| `/courses/{id}/chapters` | Teacher, Admin | Manage chapters for one course |
| `/chapters/create` | Teacher, Admin | Create chapter |
| `/chapters/{id}/edit` | Teacher, Admin | Edit chapter |
| `/benchmark` | Teacher, Admin | Benchmark dashboard |
| `/architecture` | Teacher, Admin | Architecture explanation |
| `/admin/users` | Admin | User management |

## API Endpoints

| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/api/courses` | Student, Teacher, Admin | List courses |
| GET | `/api/courses/current` | Student, Teacher, Admin | Default/current course |
| GET | `/api/courses/{id}/chapters` | Student, Teacher, Admin | Chapters for a course |
| GET | `/api/documents` | Student, Teacher, Admin | Document list with status/progress |
| GET | `/api/documents/{id}/chunks` | Student, Teacher, Admin | Chunks for one document |
| GET | `/api/chat/{sessionId}` | Student, Teacher, Admin | Chat history for current user/session |
| POST | `/api/evaluations/run` | Teacher, Admin | Run benchmark with optional strategy/model, max 50 questions |
| POST | `/api/evaluations/run-full` | Teacher, Admin | Full comparative benchmark (all strategies × all models) |
| GET | `/api/evaluations/results` | Teacher, Admin | Evaluation results with research metadata |
| GET | `/api/evaluations/export` | Teacher, Admin | Export results as JSON |

## SignalR Hub

Hub URL:

```text
/chatHub
```

Client-to-server methods:

| Method | Purpose |
|---|---|
| `JoinSession(sessionId)` | Join a chat session group |
| `SendMessage(sessionId, courseId, modelType, text)` | Send a question for the selected course |
| `ClearSession(sessionId)` | Clear the current user's session history |

Server-to-client events:

| Event | Purpose |
|---|---|
| `MessageReceived` | New user/bot message |
| `MessageFailed` | Chat generation failed |
| `SessionCleared` | Session history was cleared |

Supported `modelType` values:

```text
rag_standard
fine_tuned_only
rag_hybrid
```

## Document Indexing Progress

Uploaded documents are indexed by `BackgroundIndexingService`.

Progress fields are stored in SQL Server:

| Field | Purpose |
|---|---|
| `IndexStatus` | `Pending`, `Processing`, `Indexed`, `Failed` |
| `IndexProgressPercent` | `0` to `100` |
| `IndexStage` | Human-readable current stage |
| `IndexError` | Failure message when indexing fails |

Default progress stages:

| Stage | Percent |
|---|---:|
| Queued | 0 |
| Preparing document | 10 |
| Chunking text | 30 |
| Saving chunks | 50 |
| Creating embeddings | 60-95 |
| Finalizing | 98 |
| Indexed | 100 |

The Documents page displays the status badge, progress percentage, stage text, and progress bar.

## Fine-tuned Endpoint Contract

Request body:

```json
{
  "sessionId": "string",
  "courseCode": "PRN222",
  "question": "string",
  "history": [
    { "role": "user", "content": "string" },
    { "role": "assistant", "content": "string" }
  ]
}
```

Response body:

```json
{
  "answer": "string",
  "modelName": "string",
  "latencyMs": 123
}
```

## Manual Verification Checklist

Run after code changes:

```powershell
dotnet build .\Prn222Chatbot.sln
dotnet ef database update --project .\src\DataAccessLayer --startup-project .\src\PresentationLayer
dotnet sln .\Prn222Chatbot.sln list
dotnet list .\src\BusinessLayer\BusinessLayer.csproj reference
dotnet list .\src\DataAccessLayer\DataAccessLayer.csproj reference
rg "AppDbContext|Microsoft.EntityFrameworkCore|_db\\." src/PresentationLayer/Controllers src/PresentationLayer/Hubs src/BusinessLayer
rg "@inject\\s+.*(DbContext|AppDbContext)" src/PresentationLayer/Views
rg "AddSingleton<.*DbContext|AddSingleton\\(.*DbContext" src/PresentationLayer/Program.cs
rg "ApiKey|hf_" src/PresentationLayer/appsettings.json src/PresentationLayer/appsettings.Development.json
```

Expected:

- Build has `0 Warning(s)` and `0 Error(s)`.
- `DataAccessLayer` has no project references.
- `BusinessLayer` references only `DataAccessLayer`.
- Controllers, Hubs, and BusinessLayer do not use EF Core or `AppDbContext` directly.
- Razor Views do not inject `AppDbContext`.
- `AppDbContext` is not registered as singleton.
- Committed appsettings files do not contain API keys.

Suggested browser checks:

- Anonymous user is redirected to `/account/login`.
- Student can open `/chat`, `/documents`, and `/documents/{id}`.
- Student cannot upload/delete documents or open `/courses`, `/benchmark`, `/architecture`.
- Teacher can upload documents and manage courses/chapters.
- Admin can open `/admin/users`.
- Upload shows loading feedback.
- Indexing shows progress percent and stage.
- Document Details shows extracted text, chunks, embeddings, and errors.
- Chat uses selected course for retrieval.
- Benchmark runs up to 5 questions.

## Academic Notes

- SQL Server stores embeddings as JSON for assignment/demo purposes.
- This is not a production vector database.
- Lexical retrieval remains as a no-key fallback for classroom demos.
- The seeded PRN222 data provides a baseline course, chapters, documents, and benchmark questions.
- Deeper RAG answer quality depends on uploaded course materials.
