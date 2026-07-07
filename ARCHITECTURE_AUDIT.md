# Architecture Audit - Current State

Date: 2026-07-07

## Target Diagram

The codebase is aligned to the updated architecture diagram:

```text
User
  -> PresentationLayer
     Controllers, API Controllers, SignalR Hub, Razor Views
  -> BusinessLayer
     Services, AI Clients, Background Worker, DTOs
  -> DataAccessLayer
     Repositories, AppDbContext, EF Core
  -> SQL Server
```

## Code-Level Boundary Checks

| Rule | Current state |
|---|---|
| `PresentationLayer` references only `BusinessLayer` | Pass |
| `Program.cs` does not import `DataAccessLayer` or EF Core directly | Pass |
| Controllers, API Controllers, and Hubs call Business services | Pass |
| Business service interfaces and DTOs do not expose DAL entity/enum types | Pass |
| Business services do not inject or query `AppDbContext` directly | Pass |
| DAL owns repositories, EF entities, `AppDbContext`, migrations, and SQL Server access | Pass |
| External AI calls go through AI client abstractions | Pass |
| Background indexing runs through BusinessLayer hosted services | Pass |
| Mutating JSON API endpoints use antiforgery validation | Pass |

## Important Implementation Notes

- `BusinessLayer.DependencyInjection` is the composition bridge. It wires EF Core, ASP.NET Identity, repositories, services, AI clients, and hosted services.
- `PresentationLayer.Program.cs` stays thin and calls `AddApplicationServices(builder.Configuration)`.
- `PresentationLayer` does not reference `DataAccessLayer.csproj`.
- `ChatModelType` is a BusinessLayer enum used by `IChatService`; it is mapped internally to the DAL `ModelType` enum before persistence.
- `UserAdminService` lists user roles through `IUserAdminRepository` instead of joining Identity tables through `AppDbContext`.
- Razor Views receive ViewModels/DTOs, not EF entities.

## Verification Commands

```powershell
dotnet build .\Prn222Chatbot.sln --no-restore
dotnet list .\src\PresentationLayer\PresentationLayer.csproj reference
dotnet list .\src\BusinessLayer\BusinessLayer.csproj reference
dotnet list .\src\DataAccessLayer\DataAccessLayer.csproj reference
rg "DataAccessLayer|AppDbContext|Microsoft.EntityFrameworkCore" src/PresentationLayer -g "*.cs" -g "*.csproj"
rg "AppDbContext|_db\." src/BusinessLayer/Services -g "*.cs"
rg "DataAccessLayer" src/BusinessLayer/DTOs
rg "DataAccessLayer" src/BusinessLayer/Services -g "I*.cs"
```

Expected:

- Build: `0 Warning(s)`, `0 Error(s)`.
- `PresentationLayer` references only `BusinessLayer`.
- `BusinessLayer` references `DataAccessLayer`.
- `DataAccessLayer` has no project references.
- DAL/EF symbols do not appear in `PresentationLayer` source.
- Business service classes do not inject or query `AppDbContext` directly.
- Business DTOs and service interfaces do not expose DAL types.

## Remaining Non-Architecture Notes

- SQL Server still stores embeddings as JSON for assignment/demo scope. This is not a production vector database design.
- `IndexingQueue` is in-memory and unbounded. This is acceptable for demo scope; production should use bounded queue/backpressure or an external job queue.
- `DatabaseBootstrapper` applies migrations and seed data on startup. This is convenient for local/demo scope; production should run migrations as a separate deployment step.
