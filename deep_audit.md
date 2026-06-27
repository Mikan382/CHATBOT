# Deep Audit: PRN222 RAG Chatbot

> **Ngày**: 26/06/2026  
> **Phạm vi**: Rà soát 100% source files — entities, repositories, services, AI clients, controllers, views, JS, CSS, DI, migrations, seed data, background services, DTOs, ViewModels, validation, .csproj, .gitignore, README, checklist, global.json.  
> **Mục tiêu**: Kiểm tra tính đúng đắn của logic nghiệp vụ, luồng dữ liệu, và tính hoàn chỉnh của hệ thống — không chỉ UI/UX bề mặt.

---

## Mục lục

- [1. CRITICAL — Benchmark cho Kết quả Sai](#1-critical--benchmark-cho-kết-quả-sai)
- [2. SEVERE — Logic Sai / Thiếu Nghiêm trọng](#2-severe--logic-sai--thiếu-nghiêm-trọng)
- [3. MAJOR — Thiếu Features Cơ bản](#3-major--thiếu-features-cơ-bản)
- [4. MODERATE — Vấn đề Kỹ thuật](#4-moderate--vấn-đề-kỹ-thuật)
- [5. MINOR — Polish](#5-minor--polish)
- [6. Tổng kết](#6-tổng-kết)

---

## 1. CRITICAL — Benchmark cho Kết quả Sai

### C1. Benchmark Chunking Strategy Hoàn toàn Giả

**Mô tả**: `EvaluationService.EvaluateQuestionAsync` nhận parameter `chunkingStrategy` nhưng **không bao giờ sử dụng** nó để chunk text. Chỉ ghi vào metadata.

**Bằng chứng chi tiết:**

1. Upload document → `DocumentIndexingService.IndexAsync` (dòng 46) luôn dùng DI-injected `TextChunker` (paragraph, 800-1200 chars).
2. `Program.cs` dòng 50: `builder.Services.AddSingleton<TextChunker>()` — chỉ register `TextChunker`, **không register** `FixedSizeChunker` hay `SentenceChunker`.
3. `EvaluationService.cs` dòng 151: `RetrieveWithModelAsync(question.Question, null, 3, embeddingModelName, ct)` — **không truyền `chunkingStrategy` vào retrieval**.
4. `chunkingStrategy` parameter chỉ được lưu vào `result.ChunkingStrategy` (metadata) tại dòng 142.

**Hệ quả**: Dashboard chart "RAGAS by Chunking Strategy" cho 3 bars gần giống nhau → người đọc nghĩ 3 strategies tương đương → thực tế cả 3 đều dùng cùng 1 bộ paragraph chunks.

**File liên quan:**
- `src/BusinessLayer/Indexing/DocumentIndexingService.cs` (dòng 46)
- `src/PresentationLayer/Program.cs` (dòng 50)
- `src/BusinessLayer/Services/EvaluationService.cs` (dòng 142, 151)
- `src/BusinessLayer/Indexing/FixedSizeChunker.cs` — tồn tại nhưng không được gọi
- `src/BusinessLayer/Indexing/SentenceChunker.cs` — tồn tại nhưng không được gọi

**Cần sửa**: Benchmark phải thực sự re-chunk text bằng strategy tương ứng, tạo temporary embeddings (hoặc retrieve lexical) cho từng strategy, rồi mới evaluate.

---

### C2. Benchmark Embedding Model Comparison Sai

**Mô tả**: Khi indexing, document embeddings chỉ được tạo bằng **1 model duy nhất** (`multilingual-e5-base`). Khi benchmark với model khác, retrieval trả 0 results rồi fallback.

**Luồng thực tế khi benchmark model "phobert-base":**

1. `RetrievalService.cs` dòng 57-62: Gọi `RetrieveWithEmbeddingsAsync` với phobert client.
2. `RetrievalService.cs` dòng 101-102: Query embedding bằng phobert, rồi `ListByModelWithChunksAsync("phobert-base", ...)`.
3. `IDocumentEmbeddingRepository.cs` dòng 48: Filter `WHERE ModelName == "phobert-base"` → **trả 0 results** (vì DB chỉ có `multilingual-e5-base` embeddings).
4. `RetrievalService.cs` dòng 63-64: `results.Count > 0` → false → fall through.
5. `RetrievalService.cs` dòng 74-89: Fallback sang default embedding client → dùng `multilingual-e5-base` → **3/4 models cho kết quả giống model 1**.
6. Hoặc nếu default cũng fail → dòng 91: fallback sang **lexical retrieval** → benchmark thực chất là benchmark lexical search.

**File liên quan:**
- `src/BusinessLayer/Retrieval/RetrievalService.cs` (dòng 55-91)
- `src/DataAccessLayer/Repositories/IDocumentEmbeddingRepository.cs` (dòng 41-58)
- `src/BusinessLayer/Indexing/DocumentIndexingService.cs` (dòng 95-100)

**Cần sửa**: Khi indexing, phải tạo embeddings cho **tất cả configured models** (hoặc khi benchmark, tạo on-the-fly).

---

### C3. Không có Seed Documents → Benchmark 0 Kết quả

**Mô tả**: `Prn222SeedData.SeedAsync` tạo 1 Course, 8 Chapters, 50 Questions — nhưng **0 Documents, 0 Chunks, 0 Embeddings**.

**Hệ quả**:
- Cài mới → Chat hỏi bất kỳ câu gì → retrieval trả 0 chunks → "documents insufficient"
- Benchmark chạy → retrieval trả 0 chunks → RAG answer = "No relevant context" → RAGAS scores = 0
- Hệ thống **không demo được gì** cho đến khi user tự upload tài liệu

**File liên quan:**
- `src/DataAccessLayer/Data/Seed/Prn222SeedData.cs`

**Cần sửa**: Seed ít nhất 1-2 document mẫu (ví dụ file .txt chứa nội dung cơ bản về PRN222) hoặc bao gồm sample documents trong repo.

---

## 2. SEVERE — Logic Sai / Thiếu Nghiêm trọng

### S1. TextNormalizer Hủy Dấu Tiếng Việt

**Mô tả**: `TextNormalizer.Normalize()` xóa tất cả Unicode NonSpacingMark → loại bỏ toàn bộ dấu tiếng Việt.

```csharp
// TextNormalizer.cs dòng 16-18
var category = CharUnicodeInfo.GetUnicodeCategory(ch);
if (category == UnicodeCategory.NonSpacingMark) continue; // ← Xóa TẤT CẢ diacritics
```

**Hệ quả**: `Normalize("hướng dẫn")` → `"huong dan"`. Lexical search mất accuracy vì "hướng", "huống", "hương" đều thành "huong".

**File liên quan:**
- `src/BusinessLayer/Retrieval/TextNormalizer.cs` (dòng 10-28)

---

### S2. Test Questions Tiếng Anh

**Mô tả**: 50 câu test set tại `Prn222SeedData.cs` đều bằng tiếng Anh. Ví dụ: `"What is async/await used for in C#?"`. Đề bài yêu cầu **"nghiên cứu và so sánh hiệu quả... trong bối cảnh tiếng Việt"**.

**File liên quan:**
- `src/DataAccessLayer/Data/Seed/Prn222SeedData.cs` (dòng 91-158)

**Cần sửa**: Viết lại 50 câu hỏi bằng tiếng Việt (hoặc ít nhất song ngữ) để benchmark phản ánh đúng "bối cảnh tiếng Việt".

---

### S3. Chat Session History — Không có UI

**Mô tả**: Backend lưu `ChatSession` entity với `Title`, `UpdatedAtUtc`. Nhưng:
- Không có API list sessions
- Không có sidebar phiên cũ
- Mỗi lần vào `/chat` tạo session mới (`Guid.NewGuid()` tại `ChatController.cs` dòng 29)
- Không quay lại phiên cũ được
- Chỉ "Clear" messages, không xóa hẳn session

**File liên quan:**
- `src/PresentationLayer/Controllers/ChatController.cs` (dòng 29)
- `src/DataAccessLayer/Repositories/IChatRepository.cs` — thiếu `ListSessionsAsync`
- `src/PresentationLayer/Views/Chat/Index.cshtml` — thiếu sidebar
- `src/PresentationLayer/wwwroot/js/chat.js` — thiếu session management

---

### S4. Chat History Bao gồm Message Hiện tại → Prompt Lặp

**Mô tả**: User message được lưu vào DB **trước** khi build history context. History load bao gồm message vừa lưu → prompt chứa câu hỏi 2 lần.

```csharp
// ChatService.cs dòng 64-66
await _chatRepository.AddMessageAsync(userMessage, cancellationToken);  // ← Lưu trước
var history = await BuildHistoryAsync(sessionId, userId, cancellationToken);  // ← Load SAU KHI đã lưu
```

`BuildHistoryAsync` gọi `ListRecentMessagesAsync(sessionId, userId, 12)` → load 12 messages gần nhất **bao gồm cả message user vừa gửi**. Prompt gửi Gemini sẽ có:
- `history: [..., user: "What is async?"]` (từ history)
- `question: "What is async?"` (từ prompt template)

→ Câu hỏi bị lặp 2 lần trong prompt, lãng phí tokens và có thể gây confuse cho LLM.

**File liên quan:**
- `src/BusinessLayer/Services/ChatService.cs` (dòng 64-66)

---

### S5. Benchmark Chạy Đồng bộ → Timeout HTTP

**Mô tả**: Full benchmark chạy synchronous trong 1 HTTP request.

```csharp
// BenchmarkController.cs dòng 62-67
[HttpPost("/api/evaluations/run-full")]
public async Task<IActionResult> RunFull(...) {
    var results = await _evaluationService.RunFullBenchmarkAsync(limit, ct);
    return Json(new { success = true, count = results.Count, results });
}
```

Full benchmark = `50 questions × 3 strategies × 4 models = 600 evaluations`. Mỗi eval gọi Gemini 2 lần (generate + score) = **1200 API calls**. Chạy synchronous trong HTTP request → browser timeout (thường 2-5 phút), Kestrel timeout, hoặc SignalR disconnect.

`benchmark.js` dòng 104-129: Chỉ `await fetch(...)` rồi chờ — không có progress, không có timeout handling.

**File liên quan:**
- `src/PresentationLayer/Controllers/BenchmarkController.cs` (dòng 62-67)
- `src/PresentationLayer/wwwroot/js/benchmark.js` (dòng 104-129)

---

### S6. Seed Users Không tạo được trên Fresh Install

**Mô tả**: Password cho seed users phải được set qua `dotnet user-secrets`. Nếu chưa set, bootstrapper im lặng skip.

```csharp
// DatabaseBootstrapper.cs dòng 80-84
var password = configuration[$"SeedUsers:{key}:Password"];
if (string.IsNullOrWhiteSpace(password)) return; // ← Skip silently
```

`appsettings.json` dòng 45-57: Không có `Password` field nào. User **bắt buộc** phải set `dotnet user-secrets` TRƯỚC KHI chạy lần đầu, nếu không: 0 users được tạo → không ai login được → không có error message nào.

**File liên quan:**
- `src/DataAccessLayer/Data/DatabaseBootstrapper.cs` (dòng 80-84)
- `src/PresentationLayer/appsettings.json` (dòng 45-57)

---

### S7. checklist.md Đánh 100% Hoàn thành Nhưng Không Đúng

**Mô tả**: File `checklist.md` ở root repo kết luận:

```
| Tổng | 18 | 18 | 100% |
```

Đánh dấu **✅** cho "Benchmark nhiều chunking strategy" (3.2) và "Benchmark nhiều embedding model" (3.3) — nhưng thực tế C1 và C2 ở trên cho thấy cả 2 đều **không hoạt động đúng**. Checklist tạo ấn tượng sai rằng hệ thống hoàn chỉnh.

**File liên quan:**
- `checklist.md` (dòng 130-139)

---

### S8. DOCX Extraction Kém Chất lượng

**Mô tả**: DOCX extraction dùng `InnerText` — trả text **dính liền không spacing** giữa paragraphs, tables, headings.

```csharp
// DocumentTextExtractor.cs dòng 53-54
private static string ExtractDocx(Stream stream) {
    using var document = WordprocessingDocument.Open(stream, false);
    return document.MainDocumentPart?.Document?.Body?.InnerText ?? "";
}
```

**Hệ quả**: Output ví dụ: `"Chapter 1IntroductionThis is paragraph one.Table data"` → chunking trên text dính cho kết quả rất kém.

**So sánh**:
- PDF extractor (`PdfPig`): text theo page có spacing đúng ✅
- PPTX extractor: dùng `AppendLine()` tách slide ✅
- DOCX: **duy nhất bị lỗi** ❌

**File liên quan:**
- `src/BusinessLayer/Parsing/DocumentTextExtractor.cs` (dòng 51-55)

**Cần sửa**: Iterate qua `Paragraph` elements, join bằng `\n\n` thay vì dùng `InnerText`.

---

### S9. EvaluationQuestion Không Liên kết với Documents

**Mô tả**: `EvaluationQuestion` có `ChapterId` nhưng **không liên kết với Document cụ thể nào**. Ground truth viết tay (trong seed data) nhưng không kiểm tra xem document có chứa thông tin tương ứng không.

**Hệ quả**: Nếu không upload document chứa nội dung về "async/await in C#", RAG sẽ retrieve 0 chunks → faithfulness = 0 → nhưng ground truth nói đúng → RAGAS score **không phản ánh chất lượng retrieval** mà phản ánh thiếu tài liệu.

**File liên quan:**
- `src/DataAccessLayer/Entities/EvaluationQuestion.cs` (dòng 6)

---

### S10. UserAdminService.ListAsync() có N+1 Query

**Mô tả**: Load tất cả users rồi gọi `GetRolesAsync()` cho **từng user** → N+1 query.

```csharp
// UserAdminService.cs dòng 19-26
var users = _userManager.Users.OrderBy(x => x.Email).ToList();
foreach (var user in users) {
    var roles = await _userManager.GetRolesAsync(user);  // ← 1 query per user
```

50 users = 51 queries. Không dùng eager loading hoặc batch query.

**File liên quan:**
- `src/BusinessLayer/Services/UserAdminService.cs` (dòng 17-36)

---

### S11. ListIndexedChunksAsync Load TẤT CẢ Chunks vào Memory

**Mô tả**: Lexical retrieval load **tất cả chunks** + Include Document + Chapter + Course vào memory, rồi tính score trong C#.

```csharp
// IDocumentRepository.cs dòng 136-151
public async Task<IReadOnlyList<DocumentChunk>> ListIndexedChunksAsync(Guid? courseId, ...) {
    return await query.AsNoTracking().ToListAsync(cancellationToken);
}
```

Với 1000+ chunks → memory spike, slow response.

**File liên quan:**
- `src/DataAccessLayer/Repositories/IDocumentRepository.cs` (dòng 136-152)

---

### S12. ContentText Lưu Trực tiếp trong SQL Row

**Mô tả**: `Document.ContentText` (có thể vài MB) lưu trực tiếp trong row SQL Server (nvarchar(max)). `ListWithChapterAndChunksAsync` load **TẤT CẢ documents** kèm `ContentText` mà không có Select projection → memory spike khi list documents.

**File liên quan:**
- `src/DataAccessLayer/Entities/Document.cs` (dòng 15)
- `src/DataAccessLayer/Repositories/IDocumentRepository.cs` (dòng 37-71)

---

### S13. global.json SDK 9.0.304 nhưng Target net8.0

**Mô tả**: `global.json` pin SDK `9.0.304` nhưng tất cả `.csproj` files target `net8.0`. Hoạt động nhờ `rollForward: latestFeature` nhưng có thể gây confusion khi collaborate.

**File liên quan:**
- `global.json` (dòng 3)
- `src/PresentationLayer/PresentationLayer.csproj` (dòng 4)

---

## 3. MAJOR — Thiếu Features Cơ bản

### M1. Chat: Loading Indicator

Sau khi gửi tin nhắn, `chat.js` dòng 66-75 gọi `connection.invoke("SendMessage", ...)` rồi không có gì. Không có spinner, typing dots, hay bất kỳ feedback nào cho user trong khi Gemini xử lý (có thể 3-10 giây).

**File**: `src/PresentationLayer/wwwroot/js/chat.js`

### M2. Chat: Markdown Rendering

`chat.js` dòng 23: `body.textContent = message.content` → Plain text. Gemini thường trả lời có `**bold**`, `- lists`, `` `code` ``, headings... tất cả hiện raw text.

**File**: `src/PresentationLayer/wwwroot/js/chat.js` (dòng 23)

### M3. Chat: Không Disable khi Gemini Missing

`Chat/Index.cshtml` dòng 40-42 hiện alert "not available" nhưng input field + Send button vẫn enabled. User gửi → exception.

**File**: `src/PresentationLayer/Views/Chat/Index.cshtml` (dòng 40-42)

### M4. Documents: Re-index Failed Documents

Không có nút "Retry" cho document có `IndexStatus == Failed`. Phải delete rồi re-upload.

### M5. Documents: Pagination

`DocumentService.GetIndexDataAsync` → `ListWithChapterAndChunksAsync` load toàn bộ documents + chunks vào memory. Với 100+ documents, mỗi cái có 10-50 chunks → memory spike.

**File**: `src/BusinessLayer/Services/DocumentService.cs` (dòng 32-42)

### M6. Account: Đổi mật khẩu

Không có endpoint nào cho change password. User không đổi được password sau khi tạo.

### M7. Account: Register

Chỉ login. Muốn tạo tài khoản mới phải Admin tạo qua `/admin/users/create`.

### M8. Admin: Delete User

`UserAdminService` chỉ có `SetLockoutAsync` (lock/unlock), **không có** delete user.

**File**: `src/BusinessLayer/Services/UserAdminService.cs`

### M9. Admin: Reset Password

Admin không reset được password cho user khác.

### M10. Home Page Trống

`HomeController.Index()` → `RedirectToAction("Index", "Chat")`. Không có landing page, onboarding, hay hướng dẫn sử dụng.

**File**: `src/PresentationLayer/Controllers/HomeController.cs` (dòng 16-19)

---

## 4. MODERATE — Vấn đề Kỹ thuật

### D1. RAGAS Self-Scoring Bias

Gemini **vừa sinh câu trả lời**, **vừa chấm điểm** câu trả lời đó. `RagasScorer.cs` dòng 14 inject cùng `IGeminiClient`. Đây là LLM tự đánh giá output của chính mình — có bias thiên vị.

**File**: `src/BusinessLayer/AI/RagasScorer.cs` (dòng 14)

### D2. OpenAI Client Không Normalize Vector

`OpenAiEmbeddingClient.cs` dòng 68-73: Trả vector raw, không normalize. Trong khi `HuggingFaceEmbeddingClient` (dòng 101) và `ConfigurableHuggingFaceClient` (dòng 207) đều normalize. Cosine similarity giữa normalized vs non-normalized vectors sẽ **sai scale**.

**File**: `src/BusinessLayer/AI/OpenAiEmbeddingClient.cs` (dòng 68-75)

### D3. Không có Unit Tests

Không có project test nào trong solution. Không test chunking logic, normalization, cosine similarity, hay prompt builder.

### D4. Benchmark API Không có CSRF Protection

`BenchmarkController.cs` dòng 35-36: `[HttpPost("/api/evaluations/run")]` không có `[ValidateAntiForgeryToken]`. Có thể bị CSRF attack trigger benchmark (tốn Gemini quota). Tuy nhiên đã có `[Authorize]`.

**File**: `src/PresentationLayer/Controllers/BenchmarkController.cs` (dòng 35-36)

### D5. Architecture Page Quá Đơn giản

`Architecture/Index.cshtml`: 53 dòng text thuần, không có diagram (Mermaid, SVG, hay hình minh họa kiến trúc).

**File**: `src/PresentationLayer/Views/Architecture/Index.cshtml`

### D6. Research Report Trống

`docs/research_report.md`: Template hoàn chỉnh nhưng **tất cả bảng số liệu trống** (`-`). Đây là deliverable nhưng chưa có data thực.

**File**: `docs/research_report.md`

### D7. Benchmark Dashboard Thiếu

- Chạy 600 evaluations mà UI chỉ hiện `"Running..."` → không biết 10% hay 90%
- Không có cách xóa benchmark results cũ
- Nút "Run" không bị disable đúng cách → có thể double-click trigger 2 lần
- Full benchmark dùng `prompt()` để nhập số lượng — rất thô

**File**: `src/PresentationLayer/wwwroot/js/benchmark.js`

### D8. Không Rate Limit Chat/Benchmark

User có thể spam messages liên tục → tốn Gemini quota không kiểm soát. Benchmark có thể chạy nhiều lần đồng thời.

---

## 5. MINOR — Polish

| # | Vấn đề | Chi tiết |
|---|--------|---------|
| L1 | Empty states | Bảng trống chỉ có text "No documents match" — không có illustration/CTA |
| L2 | Breadcrumbs | Không có trên trang Details, Form, Chapters |
| L3 | Toast notifications | Dùng `TempData` (mất sau redirect) thay vì toast |
| L4 | Mobile chat layout | Chat page `col-lg-3`+`col-lg-9` — sidebar stack lên trên, chat area nhỏ |
| L5 | SignalR error UX | `alert(message)` khi lỗi — rất thô |

---

## 6. Tổng kết

### Bảng tổng hợp theo Severity

| Severity | Count | IDs |
|:--------:|:-----:|-----|
| 🔴 CRITICAL | 3 | C1, C2, C3 |
| 🟠 SEVERE | 13 | S1, S2, S3, S4, S5, S6, S7, S8, S9, S10, S11, S12, S13 |
| 🟡 MAJOR | 10 | M1, M2, M3, M4, M5, M6, M7, M8, M9, M10 |
| 🔵 MODERATE | 8 | D1, D2, D3, D4, D5, D6, D7, D8 |
| ⚪ MINOR | 5 | L1, L2, L3, L4, L5 |
| **Tổng** | **39** | |

### Bảng tổng hợp theo Module

| Module | Critical | Severe | Major | Moderate | Minor | Tổng |
|--------|:--------:|:------:|:-----:|:--------:|:-----:|:----:|
| **Benchmark/Research** | 3 | 2 | 0 | 4 | 0 | **9** |
| **Chat** | 0 | 2 | 3 | 0 | 1 | **6** |
| **Documents** | 0 | 2 | 2 | 0 | 0 | **4** |
| **Data/Seed** | 0 | 3 | 0 | 0 | 0 | **3** |
| **Account/Admin** | 0 | 1 | 4 | 0 | 0 | **5** |
| **Retrieval/NLP** | 0 | 2 | 0 | 1 | 0 | **3** |
| **Performance** | 0 | 2 | 1 | 1 | 0 | **4** |
| **UX/UI** | 0 | 0 | 1 | 1 | 4 | **6** |

### Kết luận

> **Vấn đề nghiêm trọng nhất**: `checklist.md` đánh "100% hoàn thành" cho toàn bộ 18 yêu cầu, bao gồm benchmark chunking strategy và embedding model. Nhưng qua code audit, cả 2 feature này **không hoạt động đúng** — strategies không được apply vào data thực tế, models fallback về cùng 1 kết quả. Dashboard hiện charts đẹp nhưng số liệu **vô nghĩa**. Đây là vấn đề cốt lõi của sản phẩm nghiên cứu.

### Đề xuất thứ tự Fix

1. **C1 + C2**: Fix benchmark để thực sự so sánh chunking strategies và embedding models
2. **C3**: Seed documents để hệ thống hoạt động on fresh install
3. **S4 + S8**: Fix logic bugs (prompt lặp, DOCX extraction)
4. **S3**: Implement chat session history UI
5. **S1 + S2**: Fix Vietnamese text processing và test questions
6. **M1-M3**: Chat UX (loading, markdown, disable)
7. **S5**: Background job cho benchmark
8. Còn lại theo severity
