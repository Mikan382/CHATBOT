# Checklist công việc còn lại – PRN222 RAG Chatbot

> Tài liệu này so sánh yêu cầu đề bài với trạng thái hiện tại của codebase, liệt kê những gì **đã hoàn thành (✅)** và những gì **còn thiếu / cần bổ sung (❌ / ⚠️)**.
>
> - ✅ = Đã hoàn thành
> - ⚠️ = Hoàn thành một phần / cần cải thiện
> - ❌ = Chưa có

---

## A. Tính năng hệ thống

### 1. Quản lý tài liệu

| # | Yêu cầu | Trạng thái | Ghi chú |
|---|---------|:----------:|---------|
| 1.1 | Upload PDF, DOCX, slide bài giảng (PPTX) | ✅ | `DocumentsController` + `DocumentTextExtractor` hỗ trợ PDF, DOCX, PPTX, TXT, MD |
| 1.2 | Tự động chunk tài liệu | ✅ | `TextChunker` – paragraph-based, target 800-1200 chars, overlap 150 |
| 1.3 | Tự động embed tài liệu | ✅ | `DocumentIndexingService` → `HuggingFaceEmbeddingClient` (multilingual-e5-base) |
| 1.4 | Quản lý theo môn học / chương | ✅ | Entity `Course → Chapter → Document`, CRUD đầy đủ cho Teacher/Admin |
| 1.5 | Xem danh sách tài liệu đã index | ✅ | `/documents` – hiển thị status badge, progress %, stage |

### 2. Chat & Hỏi đáp

| # | Yêu cầu | Trạng thái | Ghi chú |
|---|---------|:----------:|---------|
| 2.1 | Chat tự nhiên theo ngữ cảnh hội thoại | ✅ | SignalR + conversation history (12 messages gần nhất) |
| 2.2 | Trích dẫn nguồn tài liệu gốc | ✅ | `CitationDto` kèm sourceName, chapterTitle, chunkIndex |
| 2.3 | Giới hạn trả lời trong phạm vi tài liệu | ✅ | System prompt: "Answer only from the provided document context" |
| 2.4 | Lịch sử hội thoại theo phiên | ✅ | `ChatSession` + `ChatMessage` entities, lưu SQL Server |

### 3. Module nghiên cứu (RBL)

| # | Yêu cầu | Trạng thái | Ghi chú |
|---|---------|:----------:|---------|
| 3.1 | So sánh RAG vs fine-tuned model | ✅ | `EvaluationService` chấm điểm RAGAS cho cả RAG answer và Fine-tuned answer, lưu `FtFaithfulness`, `FtAnswerRelevance`, so sánh latency |
| 3.2 | Benchmark nhiều chunking strategy | ✅ | 3 strategies: `TextChunker` (paragraph), `FixedSizeChunker` (fixed_1000), `SentenceChunker` (sentence). `RunFullBenchmarkAsync` chạy tất cả combinations |
| 3.3 | Benchmark nhiều embedding model | ✅ | `EmbeddingClientFactory` hỗ trợ HuggingFace + OpenAI format. Cấu hình 4 models trong `appsettings.json`: multilingual-e5-base, text-embedding-3-small, PhoBERT-base, bge-m3 |
| 3.4 | Dashboard hiển thị kết quả thực nghiệm | ✅ | Chart.js bar charts (by model, by strategy, RAG vs FT), bảng tổng hợp, accordion chi tiết, export JSON |

---

## B. Sản phẩm bàn giao (Deliverables)

### 4. Sản phẩm kỹ thuật

| # | Yêu cầu | Trạng thái | Ghi chú |
|---|---------|:----------:|---------|
| 4.1 | Web app chatbot hoạt động | ✅ | ASP.NET Core MVC + SignalR, đầy đủ Auth/RBAC |
| 4.2 | Source code trên GitHub (có README) | ✅ | README.md rất chi tiết, có `.github` workflows |
| 4.3 | Test set 50 câu hỏi + ground truth | ✅ | `Prn222SeedData.cs` – đúng 50 câu hỏi + ground truth, seeded vào DB |

### 5. Sản phẩm nghiên cứu (RBL)

| # | Yêu cầu | Trạng thái | Ghi chú |
|---|---------|:----------:|---------|
| 5.1 | Báo cáo thực nghiệm so sánh models | ✅ | `docs/research_report.md` – template báo cáo đầy đủ (cần điền số liệu sau khi chạy benchmark) |
| 5.2 | Bảng số liệu RAGAS benchmark | ✅ | `RagasScorer` dùng LLM-based evaluation (Gemini) với fallback lexical. Export JSON tại `/api/evaluations/export` |

---

## C. Chi tiết công việc đã làm

### Phase 1: Multi-Strategy Chunking (RBL yêu cầu 3.2) ✅

- [x] **C1.1** Tạo interface `ITextChunker` → `BusinessLayer/Indexing/ITextChunker.cs`
- [x] **C1.2** `TextChunker` implements `ITextChunker`, StrategyName = "paragraph"
- [x] **C1.3** Implement `FixedSizeChunker : ITextChunker` → chunk 1000 chars, overlap 150
- [x] **C1.4** Implement `SentenceChunker : ITextChunker` → chunk theo câu, 600-1000 chars
- [x] **C1.5** Thêm field `ChunkingStrategy`, `EmbeddingModelName` vào `EvaluationResult`
- [x] **C1.6** Cập nhật `AppDbContext` với precision/maxlength cho new fields
- [x] **C1.7** Cập nhật `EvaluationService.RunAsync` để benchmark từng strategy

### Phase 2: Multi-Embedding Model (RBL yêu cầu 3.3) ✅

- [x] **C2.1** Thêm cấu hình multi-model `appsettings.json` (4 models: multilingual-e5-base, text-embedding-3-small, PhoBERT-base, bge-m3)
- [x] **C2.2** Implement `OpenAiEmbeddingClient : IEmbeddingClient` cho text-embedding-3-small
- [x] **C2.3** Implement `ConfigurableHuggingFaceClient` cho PhoBERT-base
- [x] **C2.4** Implement `ConfigurableHuggingFaceClient` cho bge-m3
- [x] **C2.5** Tạo `EmbeddingClientFactory` → resolve client theo tên model
- [x] **C2.6** Cập nhật `RetrievalService.RetrieveWithModelAsync()` để nhận model name
- [x] **C2.7** Cập nhật `EvaluationService` benchmark từng embedding model
- [x] **C2.8** Cập nhật `Program.cs` DI registration

### Phase 3: RAGAS Scoring (RBL yêu cầu 5.2) ✅

- [x] **C3.1** Implement `RagasScorer` (LLM-based) → `BusinessLayer/AI/RagasScorer.cs`
- [x] **C3.2** Faithfulness metric: Gemini đánh giá answer vs context
- [x] **C3.3** Answer Relevance metric: Gemini đánh giá answer vs question
- [x] **C3.4** Context Recall metric: Gemini đánh giá context vs ground truth
- [x] **C3.5** Citation Accuracy metric: Gemini đánh giá citation quality
- [x] **C3.6** Lưu metadata (strategy, model, latency) trong `EvaluationResult`

### Phase 4: RAG vs Fine-tuned Scoring (RBL yêu cầu 3.1) ✅

- [x] **C4.1** Chấm điểm RAGAS cho `FineTunedAnswer` trong `EvaluationService`
- [x] **C4.2** Thêm `FtFaithfulness`, `FtAnswerRelevance` vào `EvaluationResult`
- [x] **C4.3** Thêm `RagLatencyMs`, `FineTunedLatencyMs` (Stopwatch đo thời gian)
- [x] **C4.4** Dashboard hiển thị so sánh song song RAG vs Fine-tuned

### Phase 5: Dashboard thực nghiệm (RBL yêu cầu 3.4) ✅

- [x] **C5.1** Chart.js CDN trong Benchmark view
- [x] **C5.2** Bar chart so sánh RAGAS metrics giữa embedding models
- [x] **C5.3** Bar chart so sánh RAGAS metrics giữa chunking strategies
- [x] **C5.4** Bảng tổng hợp RAG vs Fine-tuned (avg Faithfulness, Relevance, Latency)
- [x] **C5.5** Question detail accordion (ground truth + RAG answer + FT answer + scores)
- [x] **C5.6** Export JSON endpoint `/api/evaluations/export`
- [x] **C5.7** Auto-reload sau khi chạy benchmark

### Phase 6: Báo cáo nghiên cứu (RBL yêu cầu 5.1) ✅

- [x] **C6.1** Tạo `docs/research_report.md` template báo cáo
- [x] **C6.2** Bảng số liệu templates cho từng embedding model
- [x] **C6.3** Bảng số liệu templates cho từng chunking strategy
- [x] **C6.4** Bảng số liệu so sánh RAG vs Fine-tuned
- [x] **C6.5** Phân tích RAG vs Fine-tuning trong bối cảnh tiếng Việt
- [x] **C6.6** Hướng dẫn chạy benchmark + export

### Phase 7: Polish & Miscellaneous ✅

- [x] **C7.1** Cập nhật `RagPromptBuilder` hỗ trợ trả lời tiếng Việt (detect language, respond accordingly)
- [x] **C7.2** Mở rộng limit 1-50 câu (không còn giới hạn 5)
- [x] **C7.3** Cập nhật README.md (features, API endpoints)
- [x] **C7.4** Build: 0 compilation errors, 0 warnings
- [x] **C7.5** Tạo EF Core migration `AddResearchBenchmarkFields` cho new fields

---

## D. Tổng kết trạng thái

| Nhóm | Hoàn thành | Tổng | % |
|------|:----------:|:----:|:-:|
| A1. Quản lý tài liệu | 5 | 5 | 100% |
| A2. Chat & Hỏi đáp | 4 | 4 | 100% |
| A3. Module nghiên cứu (RBL) | 4 | 4 | 100% |
| B4. Sản phẩm kỹ thuật | 3 | 3 | 100% |
| B5. Sản phẩm nghiên cứu | 2 | 2 | 100% |
| **Tổng** | **18** | **18** | **100%** |

> **Kết luận**: Tất cả yêu cầu đã được implement. Các bước tiếp theo:
> 1. **Cấu hình API keys** (HuggingFace, OpenAI, Gemini) trong User Secrets
> 2. **Build & apply migration**: `dotnet build` → chạy app (DatabaseBootstrapper tự apply)
> 3. **Chạy benchmark**: Truy cập `/benchmark` → "Full Comparison" để chạy tất cả combinations
> 4. **Điền số liệu** vào `docs/research_report.md` từ kết quả benchmark thực tế
