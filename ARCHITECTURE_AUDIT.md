# Deep Architecture Audit — Codebase vs. Diagram (Code-Level)

> Kiểm tra từng file, từng dòng — **17 vấn đề** phát hiện, chia 4 nhóm.

---

## A. VI PHẠM LAYER BOUNDARY

### #1 🔴 PresentationLayer `using DataAccessLayer` trực tiếp — 30 chỗ, 17 files

Razor Pages, API Controllers, SignalR Hub, ViewModels đều `using DataAccessLayer.Entities` và `DataAccessLayer.Enums` trực tiếp — vi phạm nguyên tắc PL chỉ giao tiếp với BL qua DTO.

**Files vi phạm:**

| File | DAL types dùng |
|---|---|
| `Pages/Documents/Index.cshtml.cs` | `Chapter`, `Document`, `DocumentIndexStatus`, `UserRoleNames` |
| `Pages/Documents/Details.cshtml.cs` | `Document`, `UserRoleNames` |
| `Pages/Chat/Index.cshtml.cs` | `UserRoleNames` |
| `Pages/Courses/Index.cshtml.cs` | `UserRoleNames` |
| `Pages/Courses/Create.cshtml.cs` | `UserRoleNames` |
| `Pages/Courses/Edit.cshtml.cs` | `UserRoleNames` |
| `Pages/Courses/Chapters.cshtml.cs` | `UserRoleNames` |
| `Pages/Chapters/Create.cshtml.cs` | `UserRoleNames` |
| `Pages/Chapters/Edit.cshtml.cs` | `UserRoleNames` |
| `Pages/Benchmark/Index.cshtml.cs` | `EvaluationQuestion`, `EvaluationResult`, `UserRoleNames` |
| `Pages/AdminUsers/Index.cshtml.cs` | `UserRoleNames` |
| `Pages/Architecture/Index.cshtml.cs` | `UserRoleNames` |
| `ApiControllers/ChatApiController.cs` | `UserRoleNames` |
| `ApiControllers/BenchmarkApiController.cs` | `UserRoleNames` |
| `ApiControllers/CoursesApiController.cs` | `UserRoleNames` |
| `ApiControllers/DocumentsApiController.cs` | `UserRoleNames` |
| `Hubs/ChatHub.cs` | `UserRoleNames`, `ModelType` |

---

### #2 🔴 Business Services trả Entity thay vì DTO — Nguyên nhân gốc

9+ methods trả raw EF Entity objects cho PL, buộc PL phải `using DataAccessLayer.Entities`:

| Service | Method | Trả về Entity |
|---|---|---|
| `DocumentService` | `GetIndexDataAsync()` | `(IReadOnlyList<Chapter>, IReadOnlyList<Document>)` |
| `DocumentService` | `GetDetailsAsync()` | `Document` |
| `DocumentService` | `UploadAsync()` | `Document` |
| `CourseService` | `GetEditableAsync()` | `Course?` |
| `CourseService` | `CreateAsync()` | `Course` |
| `ChapterService` | `GetEditableAsync()` | `Chapter?` |
| `ChapterService` | `CreateAsync()` | `Chapter` |
| `EvaluationService` | `GetDashboardDataAsync()` | `(IReadOnlyList<EvaluationQuestion>, IReadOnlyList<EvaluationResult>)` |
| `EvaluationService` | `RunAsync()` | `IReadOnlyList<EvaluationResult>` |
| `EvaluationService` | `RunFullBenchmarkAsync()` | `IReadOnlyList<EvaluationResult>` |
| `ChatService` | `GetSessionAsync()` | `ChatSession?` |

Cùng Service đã có pattern đúng (trả DTO) ở một số methods — nhưng **chưa áp dụng nhất quán**.

So sánh:
- ✅ `DocumentService.ListDocumentsAsync()` → trả `DocumentApiDto`
- ❌ `DocumentService.GetDetailsAsync()` → trả `Document` entity

---

### #3 🔴 `_ViewImports.cshtml` import DAL Enums cho MỌI Razor view

```csharp
// File: Pages/_ViewImports.cshtml, dòng 4
@using DataAccessLayer.Enums
```

Đây là **infection point trung tâm** — mọi `.cshtml` file đều có access trực tiếp tới DAL types mà không cần `using` riêng.

Hệ quả trong Razor views:

| File Razor | DAL type dùng trực tiếp |
|---|---|
| `Documents/Index.cshtml:96` | `Enum.GetValues<DocumentIndexStatus>()` |
| `Documents/Index.cshtml:137` | `doc.IndexStatus` (enum), `doc.Chapter?.Course?.Code` (Entity nav) |
| `Documents/Index.cshtml:145` | `doc.Chunks.Count` — truy cập **EF navigation property** trong view |
| `Documents/Details.cshtml:21-36` | `document.IndexStatus`, `document.Chunks.Count`, `document.UploadedByUser?.Email` |
| `Documents/Details.cshtml:66-78` | `chunk.Embeddings.Count`, `chunk.Embeddings.Select(x => x.ModelName)` — **3 cấp entity navigation** |
| `Benchmark/Index.cshtml:6-35` | `result.EvaluationQuestion?.Question`, `result.FineTunedAnswer`, etc. |
| `_Layout.cshtml:30` | `UserRoleNames.Teacher`, `UserRoleNames.Admin` |

---

### #4 🟡 Business logic NẶNG trong Razor Views

`Benchmark/Index.cshtml` dòng 6-35 chứa **30 dòng data aggregation logic** trực tiếp trong view:

```csharp
// Trong .cshtml — đây là presentation logic hay business logic?
var byModel = completedResults
    .GroupBy(r => string.IsNullOrWhiteSpace(r.EmbeddingModelName) ? "default" : r.EmbeddingModelName)
    .Select(g => new {
        Model = g.Key,
        AvgFaithfulness = g.Average(x => x.Faithfulness),
        AvgRelevance = g.Average(x => x.AnswerRelevance),
        AvgRecall = g.Average(x => x.RetrievalRecall),
        AvgCitation = g.Average(x => x.CitationAccuracy),
        Count = g.Count()
    }).ToList();

var byStrategy = completedResults
    .GroupBy(r => ...)
    // ...thêm 15 dòng nữa
```

LINQ aggregation (GroupBy, Average, Select) đáng lẽ nằm trong `EvaluationService` hoặc Page Model, không phải trong `.cshtml` view.

---

### #5 🟡 `IFormFile` (ASP.NET HTTP) lọt vào Business Layer

```csharp
// File: BusinessLayer/GlobalUsings.cs, dòng 2
global using Microsoft.AspNetCore.Http;  // ← Toàn bộ BL có access HTTP types
```

`IFormFile` dùng trong method signatures của BL:

| File | Method |
|---|---|
| `DocumentService.cs:103` | `UploadAsync(Guid, Guid, IFormFile, CancellationToken)` |
| `IDocumentTextExtractor.cs:5` | `ExtractAsync(IFormFile, CancellationToken)` |
| `DocumentTextExtractor.cs:21` | `ExtractAsync(IFormFile, CancellationToken)` |

BL phụ thuộc ngược lên ASP.NET HTTP abstraction. Nên nhận `Stream` + `string fileName` + `long fileSize` thay vì `IFormFile`.

---

### #6 🟡 UserAdminService bypass Repository — dùng `AppDbContext` trực tiếp

```csharp
// File: BusinessLayer/Services/UserAdminService.cs, dòng 13-15
private readonly AppDbContext _db;
public UserAdminService(UserManager<ApplicationUser> userManager, AppDbContext db)
```

Truy vấn LINQ trực tiếp `_db.UserRoles` + `_db.Roles` ở dòng 32-37. Vi phạm diagram: BL chỉ nên giao tiếp DAL qua Repository interfaces.

---

### #7 🟡 Business logic trong Data Access Layer (Repositories)

Repositories chứa business rules — DAL chỉ nên data access:

| Repository | Business rule |
|---|---|
| `CourseRepository.DeleteAsync():105` | `throw "Cannot delete a course that still has chapters."` |
| `ChapterRepository.DeleteAsync()` | `throw "Cannot delete a chapter that still has documents or evaluation questions."` |

Quyết định "có được xóa hay không" là **business logic**, nên nằm ở Service layer.

---

## B. SECURITY & RELIABILITY

### #8 🟡 API Controllers không có CSRF protection

Tất cả 4 API controllers **không có** `[ValidateAntiForgeryToken]`:

| Controller | Has CSRF? | Mutating actions |
|---|:---:|---|
| `ChatApiController` | ❌ | `DELETE /api/chat/{id}` |
| `BenchmarkApiController` | ❌ | `POST /api/evaluations/run`, `POST /api/evaluations/run-full` |
| `CoursesApiController` | ❌ | *(read-only — OK)* |
| `DocumentsApiController` | ❌ | *(read-only — OK)* |

`DELETE /api/chat/{sessionId}` có thể bị CSRF attack — JS gọi `fetch(..., { method: "DELETE" })` không gửi antiforgery token. Auth là cookie-based → cần bảo vệ.

---

### #9 🟡 API Error Handling không nhất quán — 3/4 controllers throw 500 raw

| Controller | Error handling |
|---|---|
| `BenchmarkApiController` | Kiểm tra `ModelState`, trả `BadRequest` — nhưng `RunFull()` trả `Ok({ success: false })` |
| `ChatApiController` | **Không try-catch** — exception throw 500 raw |
| `CoursesApiController` | **Không try-catch** — exception throw 500 raw |
| `DocumentsApiController` | **Không try-catch** — exception throw 500 raw |

`InvalidOperationException` từ Services sẽ trả 500 Internal Server Error với stack trace (dev mode).

---

### #10 🟡 `ChatHub.SendMessage` — leaking internal exception messages

```csharp
// File: Hubs/ChatHub.cs, dòng 61-64
catch (Exception ex)
{
    await Clients.Caller.SendAsync("MessageFailed", ex.Message);
}
```

`ex.Message` có thể chứa SQL errors, API key errors, internal paths → thông tin nhạy cảm gửi thẳng tới browser.

---

### #11 🟡 `ToLocalTime()` trong Razor — sai timezone trên server

```csharp
// File: Documents/Index.cshtml, dòng 146
@doc.UploadedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
```

`ToLocalTime()` dùng timezone **của server**, không phải của user. Trên server production (UTC) sẽ hiển thị UTC. Nên format UTC rồi để browser/JS convert, hoặc sử dụng một timezone cố định.

---

## C. CODE QUALITY

### #12 🟡 Duplicated Code — 3 loại

**Cosine Similarity** — 2 implementations giống hệt:

| File | Method |
|---|---|
| `Retrieval/RetrievalService.cs:171-195` | `private static double Cosine(IReadOnlyList<float>, IReadOnlyList<float>)` |
| `Retrieval/BenchmarkRetrievalService.cs:153-172` | `private static double Cosine(float[], float[])` — bản copy |

**`NormalizeRequired()`** — copy-paste 3 lần:
- `CourseService.cs:139-147`
- `ChapterService.cs:101-109`
- `UserAdminService.cs:131-139`

**`CurrentUserId()`** — copy-paste 3 lần + 1 variant inline:
- `Documents/Index.cshtml.cs:109-115`
- `Hubs/ChatHub.cs:79-85`
- `ApiControllers/ChatApiController.cs:40-46`
- `Pages/Chat/Index.cshtml.cs:35` — inline `Guid.Parse(User.FindFirstValue(...)!)` (thiếu null-check)

Nên extract thành shared utility / base class.

---

### #13 🟡 Services không có Interface — 0/8

| Service | Interface? | DI Registration |
|---|:---:|---|
| `ChatService` | ❌ | `AddScoped<ChatService>()` |
| `CourseService` | ❌ | `AddScoped<CourseService>()` |
| `ChapterService` | ❌ | `AddScoped<ChapterService>()` |
| `DocumentService` | ❌ | `AddScoped<DocumentService>()` |
| `EvaluationService` | ❌ | `AddScoped<EvaluationService>()` |
| `AuthService` | ❌ | `AddScoped<AuthService>()` |
| `UserAdminService` | ❌ | `AddScoped<UserAdminService>()` |
| `BenchmarkJobRunner` | ❌ | `AddSingleton<BenchmarkJobRunner>()` |

So sánh: AI Clients **có interface** (`IGeminiClient`, `IEmbeddingClient`, `IFineTuneClient`), Repositories **có interface** — nhưng Services thì không. PL tight-coupled vào concrete classes.

---

### #14 🟡 Dead middleware — `AddSession()` + `UseSession()` không dùng

```csharp
// File: Program.cs, dòng 21, 99
builder.Services.AddSession();
// ...
app.UseSession();
```

Grep toàn project → `HttpContext.Session` = **0 kết quả**. Session middleware đăng ký nhưng không dùng → overhead vô ích (tạo session cookie cho mọi request).

---

### #15 🟡 Hardcoded "PRN222" ở Repository layer + Service

```csharp
// File: DataAccessLayer/Repositories/ICourseRepository.cs, dòng 34
.OrderBy(x => x.Code == "PRN222" ? 0 : 1)

// File: BusinessLayer/Services/EvaluationService.cs, dòng 201
var courseCode = question.Chapter?.Course?.Code ?? "PRN222";
```

Course code cứng trong code. Nếu deploy cho course khác, logic ưu tiên sẽ sai.

---

## D. SCALABILITY & PERFORMANCE

### #16 🟡 `BenchmarkJobRunner` — `Task.Run` fire-and-forget, không cancellation

```csharp
// File: BusinessLayer/Services/BenchmarkJobRunner.cs, dòng 60
_ = Task.Run(() => RunAsync(questionLimit));
```

Vấn đề:
- `CancellationToken.None` ở dòng 83 — không thể cancel benchmark đang chạy
- `Task.Run` fire-and-forget — exception nuốt im (chỉ log)
- Singleton state (`_progress`) — race condition giữa nhiều requests đọc/ghi cùng lúc (có lock nhưng granularity thô)

---

### #17 🟡 In-memory full scan cho semantic retrieval

```csharp
// File: Retrieval/RetrievalService.cs, dòng 102
var embeddings = await _embeddingRepository.ListByModelWithChunksAsync(client.ModelName, courseId, cancellationToken);
```

Load **toàn bộ embeddings** + document chunks vào memory rồi tính cosine in-memory. `RetrieveLexicalAsync()` cũng load toàn bộ chunks (có cap 200 nhưng load trước filter). Với dataset lớn sẽ OOM.

---

## Tổng kết

### 🔴 Nghiêm trọng — 3 vấn đề

| # | Vi phạm | Scope |
|---|---|---|
| 1 | PL `using DataAccessLayer` trực tiếp | 30 chỗ, 17 files |
| 2 | Services trả Entity thay DTO (**gốc rễ**) | 10+ methods |
| 3 | `_ViewImports` import DAL Enums → mọi Razor view có DAL access | Toàn bộ views |

### 🟡 Trung bình — 14 vấn đề

| # | Vi phạm | Scope |
|---|---|---|
| 4 | Business logic nặng trong Razor (.cshtml) | Benchmark view |
| 5 | `IFormFile` ASP.NET lọt vào BL | 3 files |
| 6 | UserAdminService dùng DbContext trực tiếp | 1 service |
| 7 | Business logic trong Repository (delete rules) | 2 repos |
| 8 | API Controllers không CSRF (cookie-based auth) | 2 controllers |
| 9 | API Error handling không nhất quán | 3/4 controllers |
| 10 | ChatHub leak exception message ra client | 1 hub |
| 11 | `ToLocalTime()` dùng server timezone | 2 views |
| 12 | Duplicated code (Cosine, NormalizeRequired, CurrentUserId) | 9 bản copy |
| 13 | Services không có interfaces | 0/8 services |
| 14 | Dead `AddSession()`/`UseSession()` middleware | Program.cs |
| 15 | Hardcoded "PRN222" | 2 files |
| 16 | Fire-and-forget `Task.Run` benchmark | 1 service |
| 17 | In-memory full scan retrieval | 2 methods |

### 🟢 Làm tốt

| Điểm | Chi tiết |
|---|---|
| Folder structure 3-layer | Khớp diagram |
| Repository interfaces | 6/6 repos có interface |
| AI Client abstraction | `IGeminiClient`, `IEmbeddingClient`, `IFineTuneClient` |
| CSRF trên Razor POST handlers | `[ValidateAntiForgeryToken]` trên mọi POST handler |
| Authorization | `[Authorize(Roles)]` trên mọi page/controller/hub |
| Seeding có thể override | `SeedUsers` config + `user-secrets` |
| Background indexing queue | Channel-based `IIndexingQueue` + `BackgroundService` |
| Custom validation attributes | `AllowedFileExtensions`, `MaxFileSize` |
| DbContext Fluent Config | Indexes, precision, max lengths đầy đủ |
| Cascade delete | FK cascades configured properly |
