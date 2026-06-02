# PRN222 RAG Chatbot

ASP.NET Core MVC application for a PRN222 course document chatbot. The app lets students upload course materials, index document chunks in the background, chat with RAG citations, and compare RAG answers with a custom fine-tuned model endpoint.

## Project Status

- Runtime stack: ASP.NET Core MVC / Razor Views.
- Target framework: `net8.0`.
- SDK pin: `global.json` uses .NET SDK `9.0.304` with feature roll-forward.
- Database: SQL Server LocalDB by default.
- Architecture boundary: `Controller/Hub -> Service -> Repository -> AppDbContext -> SQL Server`.
- UI language: English.

## Features

- Manage the seeded `PRN222` course and 8 syllabus chapters.
- Upload `.pdf`, `.docx`, `.txt`, and `.md` files.
- Server-side upload validation with DataAnnotations.
- Extract text from PDF/DOCX/text documents.
- Store documents, chunks, chat history, and benchmark results in SQL Server.
- Index uploaded documents through a background worker.
- Chat in real time through SignalR.
- Retrieve top document chunks with local normalized TF-IDF/cosine search.
- Generate RAG answers through Gemini with citations.
- Disable fine-tuned mode unless a real endpoint is configured.
- Run a small benchmark dashboard for RAG/fine-tuned comparison.
- Show MVC + 3-Layers workflows on the Architecture page.

## Tech Stack

| Area | Technology |
|---|---|
| Web UI | ASP.NET Core MVC, Razor Views |
| Realtime | SignalR |
| Database | SQL Server LocalDB / SQL Server 2012+ |
| ORM | EF Core |
| Background jobs | HostedService / BackgroundService |
| PDF parser | UglyToad.PdfPig |
| DOCX parser | DocumentFormat.OpenXml |
| AI generation | Gemini REST API |
| Fine-tuned model | Custom REST endpoint |

## Solution Layout

```text
Prn222Chatbot.sln
src/
  Prn222Chatbot.Web/
    Controllers/      MVC controllers and API endpoints
    Hubs/             SignalR hub
    Services/         Business logic, AI clients, indexing, retrieval
    Repositories/     Data access boundary
    Data/             AppDbContext, migrations, seed/bootstrapper
    Domain/           EF Core entities and enums
    ViewModels/       View and input models
    Views/            Razor views
    wwwroot/          CSS, JavaScript, client assets
```

## Architecture Rules

The code follows a 3-layer structure:

| Layer | Responsibility |
|---|---|
| Presentation | Razor Views, Controllers, SignalR Hub |
| Business Logic | Services, AI clients, retrieval, scoring, indexing orchestration |
| Data Access | Repositories, AppDbContext, EF Core, SQL Server |

Important constraints:

- Controllers and Hubs call Services only.
- Services handle validation, orchestration, AI/RAG flow, and DTO mapping.
- Repositories are the only layer that queries or updates `AppDbContext`.
- `AppDbContext` is registered with scoped lifetime, not singleton.
- Razor Views do not inject or query `AppDbContext`.

## Configuration

The default connection string is stored in `src/Prn222Chatbot.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=Prn222RagChatbot;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

Local secrets can be placed in a root `.env` file. Use `.env.example` as a template:

```env
GEMINI_API_KEY="YOUR_GEMINI_API_KEY"
FineTune__EndpointUrl=""
FineTune__ApiKey=""
ConnectionStrings__DefaultConnection="Server=(localdb)\\MSSQLLocalDB;Database=Prn222RagChatbot;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Notes:

- `GEMINI_API_KEY` is required for live RAG answer generation.
- `FineTune__EndpointUrl` and `FineTune__ApiKey` are optional.
- If `FineTune__EndpointUrl` is empty, fine-tuned chat and benchmark options are disabled instead of being mocked.
- `ConnectionStrings:DefaultConnection` must exist in `appsettings.json`; the app fails fast if it is missing.

## Run Locally

Restore and build:

```powershell
dotnet restore .\Prn222Chatbot.sln
dotnet build .\Prn222Chatbot.sln
```

Create or update the database:

```powershell
dotnet ef database update --project .\src\Prn222Chatbot.Web --startup-project .\src\Prn222Chatbot.Web
```

Run the web app:

```powershell
dotnet run --project .\src\Prn222Chatbot.Web --urls http://127.0.0.1:5100
```

Open:

```text
http://127.0.0.1:5100
```

## Main Pages

| Route | Purpose |
|---|---|
| `/chat` | Realtime PRN222 chatbot |
| `/documents` | Upload documents and inspect indexing status |
| `/benchmark` | RAG/fine-tuned benchmark dashboard |
| `/architecture` | MVC + 3-Layers architecture explanation |

## API Endpoints

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/courses/current` | Current course metadata |
| GET | `/api/documents` | Document list |
| GET | `/api/documents/{id}/chunks` | Chunks for one document |
| GET | `/api/chat/{sessionId}` | Chat messages for one session |
| POST | `/api/evaluations/run` | Run benchmark, max 5 questions |
| GET | `/api/evaluations/results` | Evaluation results |

## SignalR Hub

Hub URL:

```text
/chatHub
```

Client-to-server methods:

| Method | Purpose |
|---|---|
| `JoinSession(sessionId)` | Join a chat session group |
| `SendMessage(sessionId, modelType, text)` | Send a user message |
| `ClearSession(sessionId)` | Clear persisted messages for a session |

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

Use this checklist after changing code:

```powershell
dotnet build .\Prn222Chatbot.sln
rg "AppDbContext|Microsoft.EntityFrameworkCore|_db\\." src/Prn222Chatbot.Web/Controllers src/Prn222Chatbot.Web/Hubs src/Prn222Chatbot.Web/Services
rg "@inject\\s+.*(DbContext|AppDbContext)" src/Prn222Chatbot.Web/Views
rg "AddSingleton<.*DbContext|AddSingleton\\(.*DbContext" src/Prn222Chatbot.Web/Program.cs
```

Expected:

- Build has `0 Warning(s)` and `0 Error(s)`.
- Controllers, Hubs, and Services do not use EF Core or `AppDbContext` directly.
- Razor Views do not inject `AppDbContext`.
- `AppDbContext` is not registered as singleton.

Suggested browser checks:

- `/chat` renders.
- `/documents` renders.
- Upload a `.txt`, `.pdf`, and `.docx` file.
- Uploaded documents become indexed chunks.
- RAG answers include citations when relevant context exists.
- Out-of-scope questions are rejected or answered as insufficient context.
- `/benchmark` loads and can run up to 5 questions.
- `/architecture` explains MVC + 3-Layers clearly.

## Academic Notes

- Retrieval is a local TF-IDF/cosine demo, not a production vector database.
- The embedding model comparison table is an RBL comparison aid, not proof that all embedding models were executed.
- The seeded PRN222 data provides course structure and benchmark questions.
- Deeper answer quality depends on the uploaded course materials.
