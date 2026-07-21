# PRN222 Course Assistant

ASP.NET Core MVC assignment for managing course materials and answering student
questions with retrieval-augmented generation (RAG).

> Internal demo only. VNPay uses the sandbox; there is no production deployment or real-money workflow.

## Features

- Cookie authentication with `Student`, `Teacher`, and `Admin` roles, plus Student self-registration (`/Account/Register`).
- Batch Student account creation via CSV file upload (`/AdminUsers`).
- Course/chapter CRUD and assignment-based Head Teacher (`IsHead`) document & chapter management.
- Synchronous PDF, DOCX, PPTX, TXT, and MD indexing with duplicate-content detection.
- Student session-bound file attachments (up to 3 files, ≤10MB each) integrated into dual RAG retrieval.
- Admin-configured `paragraph`, `fixed`, or `sentence` chunking for new uploads.
- Vector retrieval with Hugging Face embeddings, Gemini answers, and document citations.
- SignalR chat with searchable, renamable, clearable, and deletable sessions.
- Ground-truth benchmark for comparing chunking strategies and embedding models.
- Default free package, Gemini token quota, VNPay sandbox checkout, and an Admin dashboard.

There is no Razor Pages endpoint, background worker, indexing queue, lexical/hybrid
retrieval, fine-tuned model, wallet, recurring billing, refund, or proration workflow.

## Architecture

The solution uses a traditional three-layer architecture:

```text
Runtime:
User -> PresentationLayer -> BusinessLayer -> DataAccessLayer -> SQL Server
                          -> External AI/payment APIs

Project references:
PresentationLayer -> BusinessLayer
PresentationLayer -> DataAccessLayer  (Program.cs composition root)
BusinessLayer     -> DataAccessLayer
DataAccessLayer   -> no upper layer
```

`Program.cs` registers dependencies. Controllers and `ChatHub` call Business
interfaces. Business services orchestrate repositories and external clients.
Repositories and `AppDbContext` own persistence.

## Requirements

- .NET SDK specified by [`global.json`](global.json)
- SQL Server LocalDB or SQL Server
- Gemini API key for chat and benchmark answer generation
- Hugging Face API key for indexing, retrieval, and embedding benchmarks
- Optional VNPay sandbox credentials for paid packages

## First Run

```powershell
dotnet restore .\Prn222Chatbot.sln

dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.local" --project .\src\PresentationLayer
dotnet user-secrets set "BootstrapAdmin:FullName" "System Admin" --project .\src\PresentationLayer
dotnet user-secrets set "BootstrapAdmin:Password" "ChangeMe123" --project .\src\PresentationLayer

dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_KEY" --project .\src\PresentationLayer
dotnet user-secrets set "HuggingFace:ApiKey" "YOUR_HUGGINGFACE_KEY" --project .\src\PresentationLayer

dotnet run --project .\src\PresentationLayer
```

Open <http://localhost:5096> with the default HTTP launch profile.

Startup applies pending migrations and creates one bootstrap Admin only when the
`ApplicationUsers` table is empty. It does not recreate demo users, courses, plans,
or subscriptions on later runs. After first login:

1. Create an active free package and mark it as `Default`.
2. Create Student/Teacher accounts in `/AdminUsers`.
3. Create courses, assign Teachers, add chapters, and upload documents.

Add VNPay sandbox credentials only when payment needs to be demonstrated:

```powershell
dotnet user-secrets set "VnPay:TmnCode" "YOUR_TMNCODE" --project .\src\PresentationLayer
dotnet user-secrets set "VnPay:HashSecret" "YOUR_HASH_SECRET" --project .\src\PresentationLayer
```

## Configuration

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection |
| `BootstrapAdmin:Email/FullName/Password` | First Admin for an empty database |
| `Gemini:ApiKey`, `Gemini:Model`, `Gemini:MaxOutputTokens` | Gemini generation |
| `Rag:TopK`, `Rag:MinimumSimilarityScore` | Vector retrieval limits |
| `Rag:HistoryMessageCount` | Recent chat messages included in the prompt |
| `HuggingFace:ApiKey`, `HuggingFace:ModelName` | Runtime embedding |
| `HuggingFace:Models` | Embedding models available to benchmarks |
| `VnPay:TmnCode`, `VnPay:HashSecret` | Sandbox merchant credentials |
| `VnPay:BaseUrl`, `VnPay:PaymentTimeoutMinutes` | Checkout endpoint and pending lifetime |

The VNPay return URL is generated from the current request host. VNPay cannot call
the IPN endpoint on `localhost`; local demos confirm payment through the signed
browser return.

## Subscription Rules

- Exactly one active free plan can be marked `Default`; no plan code is hardcoded.
- A Student receives the default package when the account is created or when no
  unexpired package exists.
- Quota uses Gemini `totalTokenCount` for successful Student chat generations.
  Input and output token counts are also stored for reporting.
- No relevant document means Gemini is not called and no token usage is charged.
- Clearing or deleting chat history does not restore token quota.
- Free-to-paid activation is immediate after a verified VNPay callback.
- A paid package must expire before another paid checkout can be created.
- Only one non-expired VNPay checkout may be pending for a Student.
- Price, duration, and token quota are snapshotted at activation/payment time.
- Revenue is gross successful VNPay payment value. Expiry or replacement does not
  remove revenue because refund processing is outside the demo scope.

## Main Routes

| Route | Access | Purpose |
|---|---|---|
| `/chat` | All roles | RAG chat, session history, and session file attachments |
| `/Account/Register` | Anonymous | Student account self-registration |
| `/documents` | All roles | Browse; assigned Head Teacher can upload and delete |
| `/courses` | Teacher/Admin | Course management, Head Teacher assignment, and per-course AI config |
| `/AdminUsers` | Admin | Accounts, roles, CSV batch import, lockout, password reset, deletion |
| `/benchmark` | Admin | Ground-truth and RAG experiments |
| `/subscriptions` | Student | Current package and VNPay checkout |
| `/subscriptions/dashboard` | Admin | Plans, subscriptions, payments, revenue, token usage |

## EF Core Migrations

`AppDbContextFactory` is used only by `dotnet ef` and requires an explicit
connection string:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\MSSQLLocalDB;Database=Prn222RagChatbot;Trusted_Connection=True;TrustServerCertificate=True"
dotnet ef migrations add MigrationName --project .\src\DataAccessLayer --startup-project .\src\DataAccessLayer
dotnet ef database update --project .\src\DataAccessLayer --startup-project .\src\DataAccessLayer
```

Migration files are append-only history. Embeddings are stored as JSON in SQL Server
because this assignment uses a small course dataset and in-memory cosine similarity.
Benchmark runs call external AI APIs and consume provider quota, but do not consume a
Student subscription quota.
