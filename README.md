# PRN222 RAG Chatbot

ASP.NET Core MVC application for a PRN222 course document chatbot. The app lets students upload course materials, index document chunks in the background, chat with RAG citations, and compare RAG answers with a custom fine-tuned model endpoint.

## Project Status

- Runtime stack: ASP.NET Core MVC / Razor Views.
- Target framework: `net8.0`.
- SDK pin: `global.json` uses .NET SDK `9.0.304` with feature roll-forward.
- Database: SQL Server LocalDB by default.
- Architecture boundary: `PresentationLayer -> BusinessLayer -> DataAccessLayer -> SQL Server`.
- Configuration source: `appsettings.json`, User Secrets, and environment variables; no `.env` support.
- UI language: English.

## Features

- Manage the seeded `PRN222` course and 8 syllabus chapters.
- Upload `.pdf`, `.docx`, `.pptx`, `.txt`, and `.md` files.
- Server-side upload validation with DataAnnotations.
- Extract text from PDF/DOCX/text documents.
- Store documents, chunks, chat history, and benchmark results in SQL Server.
- Index uploaded documents through a background worker.
- Chat in real time through SignalR.
- Retrieve top document chunks with Hugging Face embeddings when configured.
- Fall back to local normalized lexical search when embeddings are unavailable.
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
| DOCX/PPTX parser | DocumentFormat.OpenXml |
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
    Services/         Chat, document, course, and evaluation orchestration
    AI/               Gemini, Hugging Face embedding, and fine-tuned clients
    Indexing/         Background worker and chunking logic
    Parsing/          PDF, DOCX, PPTX, TXT, and MD text extraction
    Retrieval/        Embedding retrieval and lexical fallback
    DTOs/             Data transfer records

  DataAccessLayer/
    Repositories/     Data access boundary
    Data/             AppDbContext, migrations, seed/bootstrapper
    Entities/         EF Core entities and enums
```

## Architecture Rules

The code follows a 3-layer structure:

| Layer | Responsibility |
|---|---|
| PresentationLayer | Razor Views, Controllers, SignalR Hub, ViewModels, browser assets |
| BusinessLayer | Services, AI clients, parsing, retrieval, scoring, indexing orchestration, DTOs |
| DataAccessLayer | Repositories, AppDbContext, EF Core entities, migrations, SQL Server |

Project references:

| Project | References |
|---|---|
| `DataAccessLayer` | none |
| `BusinessLayer` | `DataAccessLayer` |
| `PresentationLayer` | `BusinessLayer`, `DataAccessLayer` |

Important constraints:

- Controllers and Hubs call Services only.
- Services handle validation, orchestration, AI/RAG flow, and DTO mapping.
- Repositories are the only layer that queries or updates `AppDbContext`.
- `AppDbContext` is registered with scoped lifetime, not singleton.
- Razor Views do not inject or query `AppDbContext`.

## Configuration

`src/PresentationLayer/appsettings.json` is committed and must contain only non-secret defaults. It stores the required database connection string and safe model settings:

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
    "ModelUrl": "https://api-inference.huggingface.co/models/intfloat/multilingual-e5-base"
  },
  "FineTune": {
    "EndpointUrl": ""
  }
}
```

Do not put real model/API keys in committed `appsettings.json` or `appsettings.Development.json`.

For local model keys, use User Secrets. The project already has `UserSecretsId` configured.

Set Gemini:

```powershell
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_KEY" --project .\src\PresentationLayer
```

Set Hugging Face for embeddings:

```powershell
dotnet user-secrets set "HuggingFace:ApiKey" "YOUR_HUGGINGFACE_KEY" --project .\src\PresentationLayer
```

Set the custom fine-tuned endpoint:

```powershell
dotnet user-secrets set "FineTune:EndpointUrl" "https://your-fine-tuned-endpoint.example/chat" --project .\src\PresentationLayer
dotnet user-secrets set "FineTune:ApiKey" "YOUR_FINE_TUNE_API_KEY" --project .\src\PresentationLayer
```

For deployment or CI, use standard ASP.NET Core environment variables:

```powershell
$env:Gemini__ApiKey="YOUR_GEMINI_KEY"
$env:HuggingFace__ApiKey="YOUR_HUGGINGFACE_KEY"
$env:FineTune__EndpointUrl="https://your-fine-tuned-endpoint.example/chat"
$env:FineTune__ApiKey="YOUR_FINE_TUNE_API_KEY"
```

Notes:

- `ConnectionStrings:DefaultConnection` must exist in `appsettings.json`; the app fails fast if it is missing.
- `Gemini:ApiKey` is required for live RAG answer generation.
- `HuggingFace:ApiKey` is required for embedding-based indexing and retrieval.
- If Hugging Face is not configured, uploaded documents are still chunked and searched with lexical fallback.
- `FineTune:EndpointUrl` and `FineTune:ApiKey` are optional.
- If `FineTune:EndpointUrl` is empty, fine-tuned chat and benchmark options are disabled instead of being mocked.
- `.env` files are not used by this application.

## Run Locally

Restore and build:

```powershell
dotnet restore .\Prn222Chatbot.sln
dotnet build .\Prn222Chatbot.sln
```

Create or update the database:

```powershell
dotnet ef database update --project .\src\DataAccessLayer --startup-project .\src\PresentationLayer
```

Run the web app:

```powershell
dotnet run --project .\src\PresentationLayer --urls http://127.0.0.1:5100
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
dotnet ef database update --project .\src\DataAccessLayer --startup-project .\src\PresentationLayer
dotnet sln .\Prn222Chatbot.sln list
dotnet list .\src\BusinessLayer\BusinessLayer.csproj reference
dotnet list .\src\DataAccessLayer\DataAccessLayer.csproj reference
rg "AppDbContext|Microsoft.EntityFrameworkCore|_db\\." src/PresentationLayer/Controllers src/PresentationLayer/Hubs src/BusinessLayer
rg "@inject\\s+.*(DbContext|AppDbContext)" src/PresentationLayer/Views
rg "AddSingleton<.*DbContext|AddSingleton\\(.*DbContext" src/PresentationLayer/Program.cs
rg "EnvFileConfiguration|\\.env.example|ConnectionStrings__DefaultConnection" . -g "!src/**/bin/**" -g "!src/**/obj/**" -g "!README.md"
rg "\"ApiKey\"|hf_" src/PresentationLayer/appsettings.json src/PresentationLayer/appsettings.Development.json
```

Expected:

- Build has `0 Warning(s)` and `0 Error(s)`.
- Controllers, Hubs, and BusinessLayer services do not use EF Core or `AppDbContext` directly.
- Solution lists exactly `PresentationLayer`, `BusinessLayer`, and `DataAccessLayer`.
- `DataAccessLayer` has no project references; `BusinessLayer` references only `DataAccessLayer`.
- Razor Views do not inject `AppDbContext`.
- `AppDbContext` is not registered as singleton.
- There is no custom `.env` loader and no `.env.example` configuration template.
- Committed `appsettings.json` does not contain API key fields.
- Database migrations are applied before testing upload/indexing.

Suggested browser checks:

- `/chat` renders.
- `/documents` renders.
- Upload a `.txt`, `.pdf`, `.docx`, and `.pptx` file.
- Uploaded documents become indexed chunks.
- With `HuggingFace:ApiKey` configured, newly indexed chunks also get stored embeddings.
- RAG answers include citations when relevant context exists.
- Out-of-scope questions are rejected or answered as insufficient context.
- `/benchmark` loads and can run up to 5 questions.
- `/architecture` explains MVC + 3-Layers clearly.

## Academic Notes

- Embedding retrieval uses Hugging Face vectors stored in SQL Server as JSON for demo purposes.
- This is not a production vector database; large-scale deployments should use a proper vector index.
- Lexical retrieval remains as a no-key fallback for classroom demos.
- The embedding model comparison table is an RBL comparison aid, not proof that all embedding models were executed.
- The seeded PRN222 data provides course structure and benchmark questions.
- Deeper answer quality depends on the uploaded course materials.
