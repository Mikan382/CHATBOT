# PRN222 Course Assistant

ASP.NET Core MVC application for managing PRN222 course materials and answering
student questions with retrieval-augmented generation (RAG).

> Internal assignment project. VNPay uses the sandbox; no real money or production deployment is involved.

## Features

- Cookie authentication with `Student`, `Teacher`, and `Admin` roles.
- Course/chapter CRUD and many-to-many teacher assignment.
- Synchronous PDF, DOCX, PPTX, TXT, and MD indexing with duplicate-content detection.
- Admin-configured `paragraph`, `fixed`, or `sentence` chunking for new uploads.
- Pure vector RAG through Hugging Face embeddings, with Gemini answers and document citations.
- SignalR chat with searchable, renamable, clearable, and deletable sessions.
- Ground-truth benchmark for comparing chunking strategies and embedding models.
- Subscription plans with per-activation question quotas and VNPay sandbox checkout.
- Admin dashboard for paid revenue, active package value, payment state, plan distribution,
  subscription activation, and active quota usage.

## Architecture

This project uses the traditional N-layer model described in
[Microsoft's common web application architectures](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures),
not Clean Architecture.

```text
Runtime request flow:
User -> PresentationLayer -> BusinessLayer -> DataAccessLayer -> SQL Server

Compile-time project references:
PresentationLayer -> BusinessLayer
PresentationLayer -> DataAccessLayer  (Program.cs composition root only)
BusinessLayer     -> DataAccessLayer
DataAccessLayer   -> none of the upper layers
```

`Program.cs` is the composition root. Controllers and the Hub call Business interfaces;
Business services call repositories and external-provider clients. Document indexing runs
inside the upload request. There is no Razor Pages endpoint, background worker, indexing
queue, lexical retrieval fallback, conversational bypass, or fine-tuned model. Every assistant
answer first retrieves vector matches from the selected course documents.

## Requirements

- .NET SDK from [`global.json`](global.json)
- SQL Server LocalDB or another SQL Server instance
- Gemini API key for answer generation and benchmark evaluation
- Hugging Face API key for document indexing, vector retrieval, and benchmarks
- Optional VNPay sandbox merchant credentials for paid packages

## Run Locally

```powershell
dotnet restore .\Prn222Chatbot.sln
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_KEY" --project .\src\PresentationLayer
dotnet user-secrets set "HuggingFace:ApiKey" "YOUR_HUGGINGFACE_KEY" --project .\src\PresentationLayer
dotnet user-secrets set "VnPay:TmnCode" "YOUR_VNPAY_TMNCODE" --project .\src\PresentationLayer
dotnet user-secrets set "VnPay:HashSecret" "YOUR_VNPAY_HASHSECRET" --project .\src\PresentationLayer
dotnet run --project .\src\PresentationLayer
```

Open <http://localhost:5096> when using the default HTTP launch profile. If VNPay
credentials are absent, paid buttons are disabled; the rest of the application remains usable.

Startup applies pending migrations and inserts demo data only when the database is empty.
Development seed passwords are explicit in `appsettings.Development.json`; User Secrets or
environment variables can override every `SeedUsers` value.

## Configuration

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection |
| `Gemini:ApiKey`, `Gemini:Model` | Gemini credential and generation model |
| `Rag:TopK` | Number of vector matches supplied to the prompt |
| `Rag:MinimumSimilarityScore` | Minimum cosine similarity for a citation |
| `Rag:HistoryMessageCount` | Recent messages supplied as conversation context |
| `HuggingFace:ApiKey`, `HuggingFace:ModelName` | Runtime embedding credential/model |
| `HuggingFace:Models` | Allowed benchmark models, endpoints, and query/passage prefixes |
| `VnPay:TmnCode`, `VnPay:HashSecret` | VNPay sandbox merchant credentials |
| `VnPay:BaseUrl`, `VnPay:PaymentTimeoutMinutes` | Sandbox checkout endpoint and pending window |
| `SeedUsers:{Role}:Email/FullName/Password` | Required values when seeding an empty database |

The VNPay browser return URL is generated from the current request host; it is not hardcoded
in configuration. Register sandbox credentials at <https://sandbox.vnpayment.vn/devreguser/>.
VNPay cannot call the IPN endpoint on `localhost`; local demos confirm payment through the
signed browser return. A public HTTPS URL is required to demonstrate server-to-server IPN.

## Demo Accounts

| Role | Email | Default development password |
|---|---|---|
| Student | `student@prn222.local` | `Prn222@123` |
| Teacher | `teacher@prn222.local` | `Prn222@123` |
| Admin | `admin@prn222.local` | `Prn222@123` |

## Main Routes

| Route | Access | Purpose |
|---|---|---|
| `/chat` | All roles | RAG chat and session history |
| `/documents` | All roles | Browse documents; Teacher/Admin can upload/delete |
| `/courses` | Teacher/Admin | Assigned-course or full course management |
| `/AdminUsers` | Admin | Account, role, lock, password, and delete controls |
| `/settings` | Admin | Global chunking configuration |
| `/benchmark` | Admin | Ground truth and RAG experiments |
| `/subscriptions` | Student | Free activation or VNPay checkout |
| `/subscriptions/dashboard` | Admin | Plans, subscriptions, payments, revenue, and usage |

## Database Migrations

`AppDbContextFactory` exists only for `dotnet ef`. It keeps the EF design package out of
`PresentationLayer` and requires an explicit connection string instead of hiding a fallback.

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\MSSQLLocalDB;Database=Prn222RagChatbot;Trusted_Connection=True;TrustServerCertificate=True"
dotnet ef migrations add MigrationName --project .\src\DataAccessLayer --startup-project .\src\DataAccessLayer
dotnet ef database update --project .\src\DataAccessLayer --startup-project .\src\DataAccessLayer
```

Migration files are append-only history. Embeddings are JSON in SQL Server because this is a
single-course assignment dataset; retrieval computes cosine similarity in memory.

## Subscription Semantics

- Quota means accepted student questions per subscription activation, not estimated LLM tokens.
- `0` quota means unlimited. Clearing/deleting chat history does not restore quota.
- Plan price, duration, and quota are snapshotted when payment/activation occurs.
- Revenue counts successful VNPay payments. Revoking access does not refund or remove revenue.
- Active package value is shown separately and must not be interpreted as collected revenue.
- Abandoned VNPay transactions older than the configured timeout are displayed as expired.

## Scope

No deployment, production billing, refund workflow, background worker, or fine-tuned model.
Benchmarks call external AI APIs and can consume provider quota.
