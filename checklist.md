# Checklist PRN222 RAG Chatbot

> - ✅ = Hoàn thành
> - ⚠️ = Hoàn thành một phần / cần cấu hình thêm
> - ❌ = Chưa có

---

## A. Tính năng hệ thống

### 1. Quản lý tài liệu

| # | Yêu cầu | Trạng thái | Ghi chú |
|---|---------|:----------:|---------|
| 1.1 | Upload PDF, DOCX, slide bài giảng (PPTX) | ✅ | `DocumentTextExtractor`: PDF (PdfPig), DOCX (OpenXml iterate Paragraph), PPTX, TXT, MD |
| 1.2 | Tự động chunk tài liệu | ✅ | 3 chunkers: `TextChunker` (paragraph), `FixedSizeChunker` (1000/150), `SentenceChunker` |
| 1.3 | Tự động embed tài liệu | ✅ | `DocumentIndexingService` → HuggingFace multilingual-e5-base (default) |
| 1.4 | Quản lý theo môn học / chương | ✅ | `Course → Chapter → Document`, CRUD đầy đủ Teacher/Admin |
| 1.5 | Xem danh sách tài liệu đã index | ✅ | `/documents`, status badge, progress %, sort mới nhất trên đầu |

### 2. Chat & Hỏi đáp

| # | Yêu cầu | Trạng thái | Ghi chú |
|---|---------|:----------:|---------|
| 2.1 | Chat tự nhiên theo ngữ cảnh hội thoại | ✅ | SignalR, history load trước khi gửi (không duplicate), 12 messages gần nhất |
| 2.2 | Trích dẫn nguồn tài liệu gốc | ✅ | `CitationDto` kèm sourceName, chapterTitle, chunkIndex |
| 2.3 | Giới hạn trả lời trong phạm vi tài liệu | ✅ | System prompt: "Answer only from the provided document context" |
| 2.4 | Lịch sử hội thoại theo phiên | ✅ | Session list sidebar, load lại khi reload, xóa session |
| 2.5 | Disable chat khi Gemini chưa cấu hình | ✅ | Input + nút Send bị disabled, cảnh báo rõ trong UI |

### 3. Module nghiên cứu (RBL)

| # | Yêu cầu | Trạng thái | Ghi chú |
|---|---------|:----------:|---------|
| 3.1 | So sánh RAG vs fine-tuned model | ⚠️ | Fine-tune endpoint để trống (cần triển khai sau). RAGAS scoring cho RAG hoạt động |
| 3.2 | Benchmark nhiều chunking strategy | ✅ | 3 strategies thực sự chunk lại in-memory, không dùng DB chunks |
| 3.3 | Benchmark nhiều embedding model | ✅ | 3 HuggingFace models: multilingual-e5-base, PhoBERT-base, bge-m3 |
| 3.4 | Dashboard hiển thị kết quả thực nghiệm | ✅ | Chart.js, bảng tổng hợp, export JSON |
| 3.5 | Benchmark chạy nền (non-blocking) | ✅ | `BenchmarkJobRunner` + progress polling 3s |

---

## B. Sản phẩm bàn giao (Deliverables)

### 4. Sản phẩm kỹ thuật

| # | Yêu cầu | Trạng thái | Ghi chú |
|---|---------|:----------:|---------|
| 4.1 | Web app chatbot hoạt động | ✅ | ASP.NET Core MVC + SignalR, Auth/RBAC 3 roles |
| 4.2 | Source code có README | ✅ | README.md với hướng dẫn cài đặt |
| 4.3 | Test set 50 câu hỏi + ground truth tiếng Việt | ✅ | `Prn222SeedData.cs` – 50 câu hỏi tiếng Việt, seed tự động |
| 4.4 | Seed 2 tài liệu mẫu tiếng Việt | ✅ | `DatabaseBootstrapper` seed chapter 2 (async) + chapter 7 (SignalR) |

### 5. Sản phẩm nghiên cứu (RBL)

| # | Yêu cầu | Trạng thái | Ghi chú |
|---|---------|:----------:|---------|
| 5.1 | Báo cáo thực nghiệm | ⚠️ | `docs/research_report.md` – template đầy đủ, **cần điền số liệu sau benchmark** |
| 5.2 | Số liệu RAGAS benchmark | ⚠️ | Export tại `/api/evaluations/export` sau khi chạy benchmark thực tế |

---

## C. Cấu hình cần thiết trước khi chạy

```jsonc
// User Secrets: dotnet user-secrets set "Gemini:ApiKey" "..."
{
  "Gemini:ApiKey": "...",
  "HuggingFace:ApiKey": "...",
  // Fine-tune endpoint (để trống, triển khai sau)
  "FineTuned:BaseUrl": ""
}
```

**Bước chạy:**
1. `dotnet build` → app tự apply migration và seed data khi start
2. Đăng nhập với `admin@prn222.edu.vn` / `Prn222@123`
3. Upload tài liệu → đợi index xong (status = Indexed)
4. Vào `/benchmark` → "Full Comparison" → benchmark chạy nền, xem progress bar
5. Sau khi xong → xuất JSON → điền vào `docs/research_report.md`

---

## D. Tổng kết trạng thái

| Nhóm | Hoàn thành | Tổng | % |
|------|:----------:|:----:|:-:|
| A1. Quản lý tài liệu | 5 | 5 | 100% |
| A2. Chat & Hỏi đáp | 5 | 5 | 100% |
| A3. Module nghiên cứu | 4 | 5 | 80% (fine-tune pending) |
| B4. Sản phẩm kỹ thuật | 4 | 4 | 100% |
| B5. Sản phẩm nghiên cứu | 0 | 2 | 0% (cần chạy benchmark) |
| **Tổng** | **18** | **21** | **86%** |
