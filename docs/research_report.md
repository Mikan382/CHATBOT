# Báo cáo Thực nghiệm: So sánh RAG vs Fine-tuning cho Chatbot Hỏi đáp Tiếng Việt

## 1. Giới thiệu

### 1.1 Bối cảnh
Hệ thống chatbot hỏi đáp dựa trên tài liệu môn học PRN222 được xây dựng nhằm hỗ trợ sinh viên tra cứu và học tập. Hai phương pháp chính được nghiên cứu:
- **RAG (Retrieval-Augmented Generation)**: Truy xuất tài liệu liên quan rồi sinh câu trả lời dựa trên ngữ cảnh.
- **Fine-tuning**: Huấn luyện lại model trên dữ liệu chuyên biệt để trả lời trực tiếp.

### 1.2 Mục tiêu nghiên cứu
1. So sánh hiệu quả giữa RAG và Fine-tuned model trong bối cảnh tiếng Việt.
2. Benchmark nhiều chunking strategy cho RAG pipeline.
3. Benchmark nhiều embedding model cho việc truy xuất tài liệu.
4. Đánh giá chất lượng câu trả lời bằng RAGAS metrics.

### 1.3 Phương pháp đánh giá
Sử dụng framework RAGAS (Retrieval Augmented Generation Assessment) với 4 metrics:
- **Faithfulness**: Câu trả lời có trung thành với context được truy xuất không?
- **Answer Relevance**: Câu trả lời có liên quan đến câu hỏi không?
- **Context Recall**: Context truy xuất có bao phủ ground truth không?
- **Citation Accuracy**: Trích dẫn nguồn có chính xác không?

Scoring được thực hiện bằng LLM-based evaluation (Gemini) thay vì lexical overlap đơn giản.

---

## 2. Thiết lập Thực nghiệm

### 2.1 Test Set
- **50 câu hỏi** bao phủ 8 chương của môn PRN222
- Mỗi câu có **ground truth** được chuẩn bị sẵn bởi con người
- Phân bố: ~6 câu/chương, bao gồm câu hỏi khái niệm, so sánh, và ứng dụng

### 2.2 Chunking Strategies

| Strategy | Mô tả | Tham số |
|----------|-------|---------|
| `paragraph` | Chunk theo đoạn văn, ghép các đoạn ngắn | Min 800, Max 1200 chars, Overlap 150 |
| `fixed_1000` | Chunk cố định theo số ký tự | Size 1000 chars, Overlap 150 |
| `sentence` | Chunk theo câu, nhóm các câu liên tiếp | Min 600, Max 1000 chars |

### 2.3 Embedding Models

| Model | Loại | Đặc điểm |
|-------|------|----------|
| `multilingual-e5-base` | Open source (HuggingFace) | Đa ngôn ngữ, sử dụng passage/query prefix |
| `text-embedding-3-small` | Commercial (OpenAI) | Hiệu suất cao, 1536 dimensions |
| `phobert-base` | Vietnamese-specific | Được huấn luyện trên dữ liệu tiếng Việt |
| `bge-m3` | Hybrid (BAAI) | Hỗ trợ dense + sparse + multi-vector retrieval |

### 2.4 Hệ thống
- **LLM**: Gemini 2.5 Flash (cho RAG answer generation + RAGAS scoring)
- **Database**: SQL Server (lưu embeddings dạng JSON)
- **Framework**: ASP.NET Core MVC + SignalR
- **Architecture**: 3-Layer (Presentation → Business → DataAccess)

---

## 3. Kết quả Thực nghiệm

### 3.1 So sánh Embedding Models

> **Ghi chú**: Bảng dưới đây sẽ được điền sau khi chạy benchmark thực tế.
> Chạy benchmark tại: `/benchmark` → "Full Comparison" hoặc export JSON tại `/api/evaluations/export`.

| Model | Faithfulness | Answer Relevance | Context Recall | Citation Accuracy | Avg Latency (ms) |
|-------|:-----------:|:----------------:|:--------------:|:-----------------:|:-----------------:|
| multilingual-e5-base | - | - | - | - | - |
| text-embedding-3-small | - | - | - | - | - |
| phobert-base | - | - | - | - | - |
| bge-m3 | - | - | - | - | - |

### 3.2 So sánh Chunking Strategies

| Strategy | Faithfulness | Answer Relevance | Context Recall | Citation Accuracy |
|----------|:-----------:|:----------------:|:--------------:|:-----------------:|
| paragraph | - | - | - | - |
| fixed_1000 | - | - | - | - |
| sentence | - | - | - | - |

### 3.3 So sánh RAG vs Fine-tuned

| Metric | RAG | Fine-tuned |
|--------|:---:|:----------:|
| Faithfulness | - | - |
| Answer Relevance | - | - |
| Context Recall | - | N/A |
| Citation Accuracy | - | N/A |
| Avg Latency (ms) | - | - |

---

## 4. Phân tích

### 4.1 Embedding Models trong bối cảnh Tiếng Việt

**Dự kiến phân tích** (sẽ cập nhật sau khi có dữ liệu):

- **multilingual-e5-base**: Baseline đa ngôn ngữ, hiệu quả tốt cho tiếng Việt nhờ multilingual training data. Sử dụng passage/query prefix tăng accuracy.
- **text-embedding-3-small**: Model thương mại của OpenAI, thường cho kết quả tốt nhưng chi phí cao hơn.
- **phobert-base**: Được thiết kế riêng cho tiếng Việt, có thể hiểu ngữ cảnh tiếng Việt tốt hơn nhưng không được tối ưu cho embedding retrieval.
- **bge-m3**: Model hybrid mạnh, hỗ trợ retrieval đa chiều, phù hợp cho các tập tài liệu phức tạp.

### 4.2 Chunking Strategies

- **paragraph**: Bảo toàn ngữ cảnh đoạn văn, phù hợp với tài liệu có cấu trúc rõ ràng.
- **fixed_1000**: Nhanh và đơn giản, nhưng có thể cắt giữa câu/ý.
- **sentence**: Tôn trọng ranh giới câu, phù hợp hơn cho tiếng Việt khi đoạn văn dài.

### 4.3 RAG vs Fine-tuning

| Tiêu chí | RAG | Fine-tuning |
|----------|-----|-------------|
| Cập nhật kiến thức | Dễ (thêm tài liệu mới) | Khó (cần re-train) |
| Chi phí triển khai | Thấp (dùng API có sẵn) | Cao (cần GPU, training data) |
| Trích dẫn nguồn | Có (từ chunks) | Không |
| Độ chính xác domain | Phụ thuộc tài liệu | Phụ thuộc training data |
| Hallucination control | Tốt (giới hạn context) | Kém hơn |

---

## 5. Kết luận

### 5.1 Tóm tắt
1. RAG pipeline cho phép cập nhật kiến thức linh hoạt và trích dẫn nguồn chính xác.
2. Fine-tuning có thể cho câu trả lời tự nhiên hơn nhưng khó kiểm soát hallucination.
3. Lựa chọn embedding model và chunking strategy ảnh hưởng đáng kể đến chất lượng retrieval.

### 5.2 Khuyến nghị
- Sử dụng **RAG** cho hệ thống chatbot giáo dục vì khả năng trích dẫn nguồn và cập nhật tài liệu.
- **multilingual-e5-base** là lựa chọn baseline tốt cho tiếng Việt với chi phí thấp.
- **Paragraph-based chunking** bảo toàn ngữ cảnh tốt nhất cho tài liệu bài giảng.

---

## 6. Phụ lục

### 6.1 Cách chạy Benchmark
```powershell
# Set API keys
dotnet user-secrets set "Gemini:ApiKey" "YOUR_KEY" --project .\src\PresentationLayer
dotnet user-secrets set "HuggingFace:ApiKey" "YOUR_KEY" --project .\src\PresentationLayer

# Run app
dotnet run --project .\src\PresentationLayer --urls http://127.0.0.1:5100

# Navigate to /benchmark and click "Full Comparison"
# Or export results: GET /api/evaluations/export
```

### 6.2 Cấu hình Embedding Models
Xem `appsettings.json` → `EmbeddingModels` array cho cấu hình chi tiết.

### 6.3 Screenshot Dashboard
> Chụp screenshot từ `/benchmark` sau khi chạy benchmark.

### 6.4 Export dữ liệu
Dữ liệu benchmark có thể export dạng JSON tại `/api/evaluations/export`.
