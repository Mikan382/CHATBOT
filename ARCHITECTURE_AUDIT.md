# Architecture Audit - Current State

Date: 2026-07-14

## Target Diagram

The codebase is aligned to the updated MVC layered architecture:

```text
User
  -> PresentationLayer
     Controllers, API Controllers, SignalR Hub, Razor Views
  -> BusinessLayer
     Services, AI Clients, DTOs, parsing, retrieval, indexing orchestration
  -> DataAccessLayer
     Repositories, AppDbContext, EF Core
  -> SQL Server
```

## Code-Level Boundary Checks

| Rule | Current state |
|---|---|
| `Program.cs` is the composition root and registers Business/DataAccess implementations | Pass |
| Controllers, Chat API, SignalR Hub, and ViewModels do not import DataAccess/EF Core | Pass |
| Controllers, Chat API, and SignalR Hub call Business services | Pass |
| Business service interfaces and DTOs do not expose DAL entity/enum types | Pass |
| Business services do not inject or query `AppDbContext` directly | Pass |
| DAL owns repositories, EF entities, `AppDbContext`, migrations, and SQL Server access | Pass |
| External AI calls go through AI client abstractions | Pass |
| Document upload/indexing is synchronous in BusinessLayer; no hosted worker/queue | Pass |
| Benchmark re-chunks in memory and persists only completed runs/results | Pass |
| Mutating Chat API endpoints use antiforgery validation and ownership checks | Pass |

## Current Scope Notes

- Auth is custom cookie auth with `ApplicationUsers`, password hashing, and role claims.
- Course teachers are managed through `CourseTeachers`.
- Chat is RAG-only through Gemini and document retrieval.
- Fine-tune code paths remain removed; RAG chunking/embedding benchmark is implemented separately.
- Admin controls the global chunking strategy through `SystemSettings`.
- Demo seed data is initialized once and is not reapplied after administrative edits.
- Internal subscriptions use manual Admin approval and statistics only; they do not gate Chat access.
- Chat groups and history queries are scoped to the authenticated user.
- SQL Server still stores embeddings as JSON for assignment/demo scope.

## Verification Commands

```powershell
dotnet build .\Prn222Chatbot.sln --no-restore
dotnet list .\src\PresentationLayer\PresentationLayer.csproj reference
dotnet list .\src\BusinessLayer\BusinessLayer.csproj reference
dotnet list .\src\DataAccessLayer\DataAccessLayer.csproj reference
rg "DataAccessLayer|AppDbContext|Microsoft.EntityFrameworkCore" src/PresentationLayer -g "*.cs" -g "*.csproj"
rg "AppDbContext|_db\." src/BusinessLayer/Services -g "*.cs"
rg "FineTune|BackgroundIndexing|IIndexingQueue|DocumentIndexStatus" src/BusinessLayer src/PresentationLayer src/DataAccessLayer/Entities src/DataAccessLayer/Repositories -g "*.cs" -g "*.cshtml" -g "*.js"
```
