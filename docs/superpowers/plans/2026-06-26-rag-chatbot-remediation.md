# PRN222 RAG Chatbot — Remediation Implementation Plan

> **For agentic workers:** Implement task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
> **Người thực thi dự kiến:** model Sonnet 4.6 ở session mới.

**Goal:** Sửa 48 vấn đề (39 từ `deep_audit.md` + 9 mới N1–N9) để (a) module nghiên cứu RBL cho số liệu THẬT, (b) hệ thống demo được on fresh install, (c) đúng "bối cảnh tiếng Việt".

**Architecture:** ASP.NET Core MVC + 3-Layers (`PresentationLayer` → `BusinessLayer` → `DataAccessLayer`). EF Core + SQL Server LocalDB. SignalR cho chat. Gemini cho generation/scoring, HuggingFace/OpenAI cho embedding.

**Tech Stack:** .NET 8, EF Core, ASP.NET Identity, SignalR, Bootstrap 5, Chart.js (benchmark dashboard).

---

## RÀNG BUỘC BẮT BUỘC (đọc trước khi code)

1. **KHÔNG viết test code** — user tự test. Mỗi task verify bằng `dotnet build` + chạy app + quan sát thủ công.
2. **KHÔNG tạo migration mới** — toàn bộ plan thiết kế để KHÔNG đổi schema. Benchmark re-chunk/re-embed làm **in-memory**. Seed dùng entity sẵn có. Nếu phát hiện buộc phải đổi schema → DỪNG, hỏi user.
3. **File encoding UTF-8 không BOM** (Windows). Khi viết seed tiếng Việt: kiểm tra ký tự hiển thị đúng, không mojibake.
4. **KHÔNG cài thêm NuGet/JS dependency** mà chưa hỏi user. Markdown render (M2) dùng formatter tự viết, không thêm lib.
5. **Commit từng task** với message rõ ràng. End commit message:
   `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`
6. Lệnh build chuẩn: `dotnet build Prn222Chatbot.sln`. Lệnh chạy: `dotnet run --project src/PresentationLayer/PresentationLayer.csproj`.

---

## BẢN ĐỒ FILE THAY ĐỔI

| Layer | File | Phase |
|---|---|---|
| Business/Retrieval | `BenchmarkRetrievalService.cs` (TẠO MỚI) | 1 |
| Business/AI | `EmbeddingClientFactory.cs` (sửa `ReadVector`, `GetByName`) | 1 |
| Business/AI | `OpenAiEmbeddingClient.cs` (normalize) | 1 |
| Business/AI | `RagasScorer.cs` (citation fallback, judge temp) | 1 |
| Business/Services | `EvaluationService.cs` (dùng BenchmarkRetrievalService, FT fair scoring) | 1 |
| Presentation | `Program.cs` (DI chunkers + service) | 1 |
| Presentation | `appsettings.json` (ApiKey placeholders) | 1 |
| DataAccess/Seed | `Prn222SeedData.cs` (docs VN + 50 câu VN) | 2 |
| DataAccess/Data | `DatabaseBootstrapper.cs` (seed user fallback password) | 2 |
| Business/Services | `ChatService.cs` (reorder history) | 3 |
| Business/Parsing | `DocumentTextExtractor.cs` (DOCX) | 3 |
| Business/AI | `GeminiClient.cs` (parse defensive) | 3 |
| Presentation/Hubs | `ChatHub.cs` (ownership check) | 3 |
| DataAccess/Repositories | `IChatRepository.cs` (list/delete/exists session) | 3,4 |
| Business/Services | `ChatService.cs` (session APIs) | 4 |
| Presentation/Controllers | `ChatController.cs` (session endpoints) | 4 |
| Presentation/Views | `Chat/Index.cshtml` (sidebar + disable) | 4 |
| Presentation/wwwroot | `js/chat.js` (sessions, loading, markdown) | 4 |
| Presentation/Controllers | `BenchmarkController.cs` (background job) | 5 |
| Presentation/wwwroot | `js/benchmark.js` (progress, reset) | 5 |
| Presentation/Controllers | `AccountController.cs`, `AdminUsersController.cs` | 6 |
| DataAccess/Repositories | `IDocumentRepository.cs` (projection, pagination) | 7 |
| Business/Services | `UserAdminService.cs` (batch roles) | 7 |
| Docs | `research_report.md`, `checklist.md`, `README.md`, `global.json` | 8 |

---

# PHASE 1 — RESEARCH VALIDITY (cụm quan trọng nhất — C1, C2, N1, N2, N3, N4, N5, D1, D2)

> Mục tiêu phase: benchmark phải thực sự thay đổi theo chunking strategy VÀ embedding model. Sau phase này, chạy benchmark cho 3 strategy × 4 model phải ra số liệu KHÁC NHAU (không còn fallback về cùng 1 kết quả).

## Task 1.1 — Fix mean-pooling cho embedding (N1)

**Vấn đề:** `ConfigurableHuggingFaceClient.ReadVector` gặp output 2D `[tokens][hidden]` (PhoBERT, BERT thô) lấy sub-array đầu = token đầu → vector vô nghĩa. Phải mean-pool.

**Files:**
- Modify: `src/BusinessLayer/AI/EmbeddingClientFactory.cs` (method `ReadVector`, ~dòng 210-243)

- [ ] **Step 1:** Thay toàn bộ method `ReadVector` trong `ConfigurableHuggingFaceClient` bằng:

```csharp
private static float[] ReadVector(System.Text.Json.JsonElement element)
{
    if (element.ValueKind != System.Text.Json.JsonValueKind.Array)
    {
        return [];
    }

    var children = element.EnumerateArray().ToList();
    if (children.Count == 0)
    {
        return [];
    }

    // 1D: [h0, h1, ...] -> already a sentence vector (sentence-transformers models)
    if (children[0].ValueKind == System.Text.Json.JsonValueKind.Number)
    {
        return children.Select(x => x.GetSingle()).ToArray();
    }

    // Array of arrays
    if (children[0].ValueKind == System.Text.Json.JsonValueKind.Array)
    {
        // 3D batch dim [[[...]]] -> descend into first batch element
        var firstInner = children[0].EnumerateArray().FirstOrDefault();
        if (firstInner.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            return ReadVector(children[0]);
        }

        // 2D token-level [tokens][hidden] -> MEAN POOL across tokens
        return MeanPool(children);
    }

    return [];
}

private static float[] MeanPool(List<System.Text.Json.JsonElement> rows)
{
    var vectors = rows
        .Where(r => r.ValueKind == System.Text.Json.JsonValueKind.Array)
        .Select(r => r.EnumerateArray().Select(x => x.GetSingle()).ToArray())
        .Where(v => v.Length > 0)
        .ToList();

    if (vectors.Count == 0)
    {
        return [];
    }

    var dim = vectors[0].Length;
    var pooled = new float[dim];
    foreach (var v in vectors)
    {
        for (var i = 0; i < dim && i < v.Length; i++)
        {
            pooled[i] += v[i];
        }
    }

    for (var i = 0; i < dim; i++)
    {
        pooled[i] /= vectors.Count;
    }

    return pooled;
}
```

`Normalize(vector)` vẫn được gọi sau `ReadVector` (dòng 207, không đổi).

- [ ] **Step 2:** Verify: `dotnet build Prn222Chatbot.sln` → 0 errors.
- [ ] **Step 3:** Commit: `fix(embedding): mean-pool token-level vectors for PhoBERT/BERT models (N1)`

## Task 1.2 — Normalize vector cho OpenAI client (D2)

**Files:**
- Read TRƯỚC: `src/BusinessLayer/AI/OpenAiEmbeddingClient.cs` (xác định method trả vector, ~dòng 68-75)
- Modify: cùng file

- [ ] **Step 1:** Thêm method `Normalize` (copy từ `ConfigurableHuggingFaceClient`):

```csharp
private static float[] Normalize(float[] vector)
{
    var magnitude = Math.Sqrt(vector.Sum(x => (double)x * x));
    if (magnitude <= 0)
    {
        return vector;
    }

    for (var i = 0; i < vector.Length; i++)
    {
        vector[i] = (float)(vector[i] / magnitude);
    }

    return vector;
}
```

- [ ] **Step 2:** Tại điểm `return` vector raw, đổi thành `return Normalize(vector);`
- [ ] **Step 3:** Build → 0 errors. Commit: `fix(embedding): L2-normalize OpenAI vectors to match other clients (D2)`

## Task 1.3 — `GetByName` fallback nhất quán (N4)

**Files:**
- Modify: `src/BusinessLayer/AI/EmbeddingClientFactory.cs` (method `GetByName`, ~dòng 46-51)

- [ ] **Step 1:** Sửa `GetByName` để khi không có `EmbeddingModels` section vẫn khớp legacy name:

```csharp
public IEmbeddingClient? GetByName(string name)
{
    var configs = GetModelConfigs();
    if (configs.Count == 0)
    {
        // Legacy mode: only the single HuggingFace model exists
        var legacyName = _configuration["HuggingFace:ModelName"] ?? "intfloat/multilingual-e5-base";
        return name.Equals(legacyName, StringComparison.OrdinalIgnoreCase)
            ? CreateHuggingFaceFromLegacyConfig()
            : null;
    }

    var config = configs.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    return config is null ? null : CreateClient(config);
}
```

- [ ] **Step 2:** Build → 0 errors. Commit: `fix(embedding): GetByName honors legacy config fallback (N4)`

## Task 1.4 — Citation fallback không inflate (N3)

**Files:**
- Modify: `src/BusinessLayer/AI/RagasScorer.cs` (dòng 125)

- [ ] **Step 1:** Đổi:

```csharp
// CŨ: decimal citation = answer.Contains("[Source", ...) || chunks.Count > 0 ? 1m : 0m;
decimal citation = answer.Contains("[Source", StringComparison.OrdinalIgnoreCase) ? 1m : 0m;
```

- [ ] **Step 2:** Build. Commit: `fix(ragas): citation fallback only credits real source markers (N3)`

## Task 1.5 — Giảm bias self-scoring (D1, nhẹ)

**Files:**
- Modify: `src/BusinessLayer/AI/RagasScorer.cs` (system instruction + prompt)

- [ ] **Step 1:** Trong `ScoreAsync`, đổi `systemInstruction` thành strict hơn và yêu cầu chấm khắt khe, độc lập với việc ai sinh câu trả lời:

```csharp
var systemInstruction = "You are an impartial, strict RAG evaluation judge. "
    + "Do NOT assume the answer is correct. Penalize any claim not supported by the retrieved context or ground truth. "
    + "Output ONLY valid JSON.";
```

- [ ] **Step 2:** Ghi chú giới hạn này vào research report (Phase 8): self-scoring bias không loại bỏ hoàn toàn, chỉ giảm. Build. Commit: `chore(ragas): stricter judge instruction to reduce self-scoring bias (D1)`

## Task 1.6 — TẠO `BenchmarkRetrievalService` (lõi C1 + C2)

**Vấn đề:** Benchmark phải re-chunk text theo từng strategy và re-embed theo từng model, IN-MEMORY, không đụng DB embeddings sản xuất. Precompute 1 lần cho mỗi (strategy, model) rồi tái dùng cho tất cả question.

**Files:**
- Create: `src/BusinessLayer/Retrieval/BenchmarkRetrievalService.cs`

**Dependencies có sẵn để tái dùng:**
- `EmbeddingClientFactory.GetByName(modelName)` → `IEmbeddingClient`
- Chunkers: `TextChunker` (StrategyName `"paragraph"`), `FixedSizeChunker` (`"fixed_1000"`), `SentenceChunker` (`"sentence"`)
- `IDocumentRepository.ListWithChapterAndChunksAsync(null, courseId, null, DocumentIndexStatus.Indexed, ct)` → `Document` có `ContentText`
- `RetrievedChunkDto` (BusinessLayer/DTOs/AppDtos.cs) — KHÔNG đổi shape

- [ ] **Step 1:** Tạo file với nội dung:

```csharp
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;
using BusinessLayer.AI;
using BusinessLayer.Indexing;
using BusinessLayer.Services;

namespace BusinessLayer.Retrieval;

/// <summary>
/// Builds an in-memory, fully comparable retrieval index for benchmarking:
/// re-chunks document text with the requested strategy and re-embeds with the
/// requested embedding model. Nothing is persisted to the production DB.
/// </summary>
public class BenchmarkRetrievalService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly EmbeddingClientFactory _embeddingClientFactory;
    private readonly IReadOnlyDictionary<string, ITextChunker> _chunkers;
    private readonly ILogger<BenchmarkRetrievalService> _logger;

    public BenchmarkRetrievalService(
        IDocumentRepository documentRepository,
        EmbeddingClientFactory embeddingClientFactory,
        IEnumerable<ITextChunker> chunkers,
        ILogger<BenchmarkRetrievalService> logger)
    {
        _documentRepository = documentRepository;
        _embeddingClientFactory = embeddingClientFactory;
        _chunkers = chunkers.ToDictionary(c => c.StrategyName, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    /// <summary>
    /// Builds an embedded index for one (strategy, model) over all Indexed documents of a course.
    /// Returns an Unavailable index (with reason) if the model is not configured.
    /// </summary>
    public async Task<BenchmarkIndex> BuildIndexAsync(Guid? courseId, string strategy, string modelName, CancellationToken cancellationToken)
    {
        if (!_chunkers.TryGetValue(strategy, out var chunker))
        {
            return BenchmarkIndex.Unavailable($"Unknown chunking strategy '{strategy}'.");
        }

        var client = _embeddingClientFactory.GetByName(modelName);
        if (client is null || !client.IsConfigured)
        {
            return BenchmarkIndex.Unavailable($"Embedding model '{modelName}' is not configured (missing API key/URL).");
        }

        var documents = await _documentRepository.ListWithChapterAndChunksAsync(
            null, courseId, null, DocumentIndexStatus.Indexed, cancellationToken);

        if (documents.Count == 0)
        {
            return BenchmarkIndex.Unavailable("No indexed documents found to benchmark.");
        }

        var entries = new List<BenchmarkChunk>();
        foreach (var document in documents)
        {
            var texts = chunker.Chunk(document.ContentText);
            for (var i = 0; i < texts.Count; i++)
            {
                var vector = await client.EmbedPassageAsync(texts[i], cancellationToken);
                entries.Add(new BenchmarkChunk(
                    document.Id,
                    document.OriginalFileName,
                    document.Chapter?.Title ?? "PRN222",
                    i + 1,
                    texts[i],
                    vector));
            }
        }

        _logger.LogInformation("Built benchmark index: strategy={Strategy} model={Model} chunks={Count}",
            strategy, modelName, entries.Count);
        return new BenchmarkIndex(client, entries);
    }
}

public sealed class BenchmarkIndex
{
    private readonly IEmbeddingClient? _client;
    private readonly IReadOnlyList<BenchmarkChunk> _chunks;

    public bool Available { get; }
    public string? UnavailableReason { get; }

    private BenchmarkIndex(IEmbeddingClient? client, IReadOnlyList<BenchmarkChunk> chunks, bool available, string? reason)
    {
        _client = client;
        _chunks = chunks;
        Available = available;
        UnavailableReason = reason;
    }

    public BenchmarkIndex(IEmbeddingClient client, IReadOnlyList<BenchmarkChunk> chunks)
        : this(client, chunks, true, null) { }

    public static BenchmarkIndex Unavailable(string reason) =>
        new(null, [], false, reason);

    public async Task<IReadOnlyList<RetrievedChunkDto>> RetrieveAsync(string query, int topK, CancellationToken cancellationToken)
    {
        if (!Available || _client is null || _chunks.Count == 0 || string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var queryVector = await _client.EmbedQueryAsync(query, cancellationToken);
        return _chunks
            .Select(c => new { Chunk = c, Score = Cosine(queryVector, c.Vector) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => new RetrievedChunkDto(
                Guid.NewGuid(),          // ephemeral chunk id (not persisted)
                x.Chunk.DocumentId,
                x.Chunk.SourceName,
                x.Chunk.ChapterTitle,
                x.Chunk.ChunkIndex,
                x.Chunk.Content,
                x.Score))
            .ToList();
    }

    private static double Cosine(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        var n = Math.Min(a.Count, b.Count);
        if (n == 0) return 0;
        double dot = 0, ma = 0, mb = 0;
        for (var i = 0; i < n; i++)
        {
            dot += a[i] * b[i];
            ma += a[i] * a[i];
            mb += b[i] * b[i];
        }
        if (ma <= 0 || mb <= 0) return 0;
        return dot / (Math.Sqrt(ma) * Math.Sqrt(mb));
    }
}

public sealed record BenchmarkChunk(
    Guid DocumentId,
    string SourceName,
    string ChapterTitle,
    int ChunkIndex,
    string Content,
    float[] Vector);
```

- [ ] **Step 2:** Build → 0 errors. Commit: `feat(research): in-memory benchmark retrieval that varies by strategy + model (C1, C2)`

## Task 1.7 — Đăng ký DI cho chunkers + service (Program.cs)

**Files:**
- Modify: `src/PresentationLayer/Program.cs` (~dòng 49-75)

- [ ] **Step 1:** Sau dòng `builder.Services.AddSingleton<TextChunker>();` thêm:

```csharp
// Chunkers for benchmark (enumerable resolves all three strategies by StrategyName)
builder.Services.AddSingleton<ITextChunker, TextChunker>();
builder.Services.AddSingleton<ITextChunker>(_ => new FixedSizeChunker(1000, 150));
builder.Services.AddSingleton<ITextChunker, SentenceChunker>();
builder.Services.AddScoped<BenchmarkRetrievalService>();
```

> Giữ nguyên `AddSingleton<TextChunker>()` cũ vì `DocumentIndexingService` inject concrete `TextChunker`. `FixedSizeChunker.StrategyName` = `"fixed_1000"` khớp `AvailableChunkingStrategies`.

- [ ] **Step 2:** Build → 0 errors. Commit: `chore(di): register chunkers + BenchmarkRetrievalService (C1)`

## Task 1.8 — EvaluationService dùng BenchmarkRetrievalService + FT fair scoring (C1, C2, N2)

**Files:**
- Modify: `src/BusinessLayer/Services/EvaluationService.cs`

- [ ] **Step 1:** Inject `BenchmarkRetrievalService` vào constructor (thêm field + param). Có thể bỏ `RetrievalService` khỏi eval nếu không còn dùng (giữ lại cũng được, không hại).

```csharp
private readonly BenchmarkRetrievalService _benchmarkRetrieval;
// ... thêm vào constructor params + gán
```

- [ ] **Step 2:** Sửa `RunFullBenchmarkAsync` để build index 1 lần / (strategy, model), tái dùng cho mọi question:

```csharp
public async Task<IReadOnlyList<EvaluationResult>> RunFullBenchmarkAsync(int questionLimit, CancellationToken cancellationToken)
{
    questionLimit = Math.Clamp(questionLimit, 1, 50);
    var questions = await _evaluationRepository.ListQuestionsForRunAsync(questionLimit, cancellationToken);
    var strategies = AvailableChunkingStrategies;
    var models = AvailableEmbeddingModels;

    var allResults = new List<EvaluationResult>();
    foreach (var strategy in strategies)
    {
        foreach (var model in models)
        {
            var index = await _benchmarkRetrieval.BuildIndexAsync(null, strategy, model, cancellationToken);
            foreach (var question in questions)
            {
                var result = await EvaluateQuestionAsync(question, strategy, model, index, cancellationToken);
                allResults.Add(result);
            }
        }
    }

    await _evaluationRepository.SaveResultsAsync(allResults, cancellationToken);
    return allResults;
}
```

- [ ] **Step 3:** Sửa `RunAsync` (single combo) tương tự — build index 1 lần rồi loop questions:

```csharp
public async Task<IReadOnlyList<EvaluationResult>> RunAsync(int limit, string? chunkingStrategy, string? embeddingModel, CancellationToken cancellationToken)
{
    limit = Math.Clamp(limit, 1, 50);
    var questions = await _evaluationRepository.ListQuestionsForRunAsync(limit, cancellationToken);
    var strategy = chunkingStrategy ?? "paragraph";
    var modelName = embeddingModel ?? _embeddingClientFactory.GetModelNames().FirstOrDefault() ?? "default";

    var index = await _benchmarkRetrieval.BuildIndexAsync(null, strategy, modelName, cancellationToken);
    var results = new List<EvaluationResult>();
    foreach (var question in questions)
    {
        results.Add(await EvaluateQuestionAsync(question, strategy, modelName, index, cancellationToken));
    }

    await _evaluationRepository.SaveResultsAsync(results, cancellationToken);
    return results;
}
```

- [ ] **Step 4:** Đổi chữ ký `EvaluateQuestionAsync` nhận `BenchmarkIndex index`, dùng `index.RetrieveAsync` thay `_retrievalService.RetrieveWithModelAsync`, và xử lý index không khả dụng + FT fair scoring (N2):

```csharp
private async Task<EvaluationResult> EvaluateQuestionAsync(
    EvaluationQuestion question,
    string chunkingStrategy,
    string embeddingModelName,
    BenchmarkIndex index,
    CancellationToken cancellationToken)
{
    var result = new EvaluationResult
    {
        Id = Guid.NewGuid(),
        EvaluationQuestionId = question.Id,
        ChunkingStrategy = chunkingStrategy,
        EmbeddingModelName = embeddingModelName,
        CreatedAtUtc = DateTime.UtcNow
    };

    // Honest signal: model not configured -> Skipped, NOT silent fallback
    if (!index.Available)
    {
        result.Status = "Skipped";
        result.ErrorMessage = index.UnavailableReason;
        result.RagAnswer = index.UnavailableReason ?? "Benchmark index unavailable.";
        return result;
    }

    try
    {
        var ragStopwatch = Stopwatch.StartNew();
        var chunks = await index.RetrieveAsync(question.Question, 3, cancellationToken);
        result.RetrievedChunksJson = JsonSerializer.Serialize(chunks);

        if (chunks.Count == 0)
        {
            result.RagAnswer = "No relevant context was found in the indexed documents.";
        }
        else
        {
            var prompt = RagPromptBuilder.BuildPrompt(question.Question, chunks, []);
            result.RagAnswer = await _geminiClient.GenerateAsync(RagPromptBuilder.BuildSystemInstruction(), prompt, cancellationToken);
        }
        ragStopwatch.Stop();
        result.RagLatencyMs = (int)ragStopwatch.ElapsedMilliseconds;

        // Fine-tuned
        if (_fineTuneClient.IsConfigured)
        {
            var ftStopwatch = Stopwatch.StartNew();
            var ft = await _fineTuneClient.GenerateAsync(
                new FineTuneRequest(Guid.NewGuid().ToString(), "PRN222", question.Question, []),
                cancellationToken);
            result.FineTunedAnswer = ft.Answer;
            ftStopwatch.Stop();
            result.FineTunedLatencyMs = (int)ftStopwatch.ElapsedMilliseconds;
        }

        // RAGAS for RAG (context = retrieved chunks)
        var ragScore = await _ragasScorer.ScoreAsync(
            question.Question, question.GroundTruth, result.RagAnswer, chunks, cancellationToken);
        result.Faithfulness = ragScore.Faithfulness;
        result.AnswerRelevance = ragScore.AnswerRelevance;
        result.RetrievalRecall = ragScore.RetrievalRecall;
        result.CitationAccuracy = ragScore.CitationAccuracy;

        // RAGAS for Fine-tuned — N2 FIX: score against ground truth as reference, not empty context
        if (!string.IsNullOrWhiteSpace(result.FineTunedAnswer))
        {
            var ftReference = new List<RetrievedChunkDto>
            {
                new(Guid.Empty, Guid.Empty, "ground_truth", "Reference", 0, question.GroundTruth, 1.0)
            };
            var ftScore = await _ragasScorer.ScoreAsync(
                question.Question, question.GroundTruth, result.FineTunedAnswer, ftReference, cancellationToken);
            result.FtFaithfulness = ftScore.Faithfulness;
            result.FtAnswerRelevance = ftScore.AnswerRelevance;
        }

        result.Status = "Completed";
    }
    catch (Exception ex)
    {
        result.Status = "Failed";
        result.ErrorMessage = ex.Message;
        if (string.IsNullOrWhiteSpace(result.RagAnswer))
        {
            result.RagAnswer = "Evaluation failed.";
        }
        _logger.LogWarning(ex, "Evaluation failed q={QuestionId} strategy={Strategy} model={Model}",
            question.Id, chunkingStrategy, embeddingModelName);
    }

    return result;
}
```

> Lưu ý: `RetrievedChunkDto` constructor là `(ChunkId, DocumentId, SourceName, ChapterTitle, ChunkIndex, Content, Score)` — đã khớp ở trên.

- [ ] **Step 5:** Build → 0 errors.
- [ ] **Step 6:** Verify thủ công (sau khi Phase 2 seed docs xong, hoặc upload tài liệu + có API key embedding): chạy benchmark `/benchmark`, run-full với limit nhỏ (2 câu). Kết quả khác nhau giữa strategy/model; model thiếu key → status `Skipped` với lý do rõ ràng. Nếu chưa có key embedding nào → toàn bộ `Skipped` (đúng kỳ vọng, không còn giả).
- [ ] **Step 7:** Commit: `feat(research): real strategy/model benchmark + fair fine-tune scoring (C1, C2, N2)`

## Task 1.9 — Surface API key requirement (N5)

**Vấn đề:** Không có `HuggingFace:ApiKey` trong config → 0 embeddings → toàn bộ RAG rơi về lexical. Phải làm điều này HIỂN THỊ chứ không im lặng.

**Files:**
- Modify: `src/PresentationLayer/appsettings.json`

- [ ] **Step 1:** Thêm placeholder rỗng (để rõ key cần set) — KHÔNG commit key thật:

```json
"HuggingFace": {
  "ApiKey": "",
  "ModelName": "intfloat/multilingual-e5-base",
  "ModelUrl": "https://router.huggingface.co/hf-inference/models/intfloat/multilingual-e5-base/pipeline/feature-extraction"
},
"OpenAI": {
  "ApiKey": ""
},
```

- [ ] **Step 2 (QUYẾT ĐỊNH: Không có OpenAI key):** Xóa entry `text-embedding-3-small` khỏi `appsettings.json` → chỉ giữ 3 HF models (`multilingual-e5-base`, `phobert-base`, `bge-m3`). Kết quả section `EmbeddingModels`:

```json
"EmbeddingModels": [
  {
    "Name": "multilingual-e5-base",
    "Type": "HuggingFace",
    "ModelUrl": "https://router.huggingface.co/hf-inference/models/intfloat/multilingual-e5-base/pipeline/feature-extraction",
    "ApiKeyConfig": "HuggingFace:ApiKey",
    "UsePassagePrefix": true
  },
  {
    "Name": "phobert-base",
    "Type": "HuggingFace",
    "ModelUrl": "https://router.huggingface.co/hf-inference/models/vinai/phobert-base/pipeline/feature-extraction",
    "ApiKeyConfig": "HuggingFace:ApiKey",
    "UsePassagePrefix": false
  },
  {
    "Name": "bge-m3",
    "Type": "HuggingFace",
    "ModelUrl": "https://router.huggingface.co/hf-inference/models/BAAI/bge-m3/pipeline/feature-extraction",
    "ApiKeyConfig": "HuggingFace:ApiKey",
    "UsePassagePrefix": false
  }
]
```

- [ ] **Step 3:** Tài liệu hoá trong README (Phase 8): set key qua user-secrets:
  ```
  dotnet user-secrets set "HuggingFace:ApiKey" "hf_xxx"
  dotnet user-secrets set "Gemini:ApiKey" "AIza..."
  ```
- [ ] **Step 4:** Commit: `chore(config): remove OpenAI model (no key), expose HF+Gemini placeholders (N5)`

---

# PHASE 2 — DEMO ENABLEMENT (C3, S2, S6, S9)

> Sau phase: cài mới → có sẵn tài liệu tiếng Việt đã index (lexical chạy ngay), 50 câu hỏi tiếng Việt, login được mà không cần secrets.

## Task 2.1 — Seed user fallback password (S6)

**Files:**
- Modify: `src/DataAccessLayer/Data/DatabaseBootstrapper.cs` (method `SeedUserAsync`, dòng 80-84)

- [ ] **Step 1:** Đổi silent-skip thành default password + log cảnh báo:

```csharp
var password = configuration[$"SeedUsers:{key}:Password"];
if (string.IsNullOrWhiteSpace(password))
{
    // Fresh-install fallback so the app is usable without user-secrets.
    // NOTE: dev-only default; override via SeedUsers:{key}:Password in production.
    password = "Prn222@123";
    Console.WriteLine($"[SEED] No password configured for '{key}'. Using default dev password 'Prn222@123' for {defaultEmail}.");
}
```

- [ ] **Step 2:** Build. Verify: xoá DB (`Prn222RagChatbot`), chạy app, login `admin@prn222.local` / `Prn222@123`. Commit: `fix(seed): default dev password so seed users always create (S6)`

## Task 2.2 — Seed tài liệu tiếng Việt + để pipeline tự index (C3, S9)

**Thiết kế:** Seed `Document` với `ContentText` tiếng Việt, `IndexStatus = Pending`, gắn `ChapterId` (Chapter 02 async) + `UploadedByUserId` = Admin. Hosted service `PendingDocumentQueueHostedService` sẵn có sẽ tự chunk + normalize (+ embed nếu có key) lúc startup. KHÔNG cần đụng `TextNormalizer` từ DataAccessLayer (tránh vi phạm layering).

> Vì seed cần Admin user id, Task 2.2 chạy SAU Task 2.1. `Prn222SeedData.SeedAsync` hiện chỉ nhận `AppDbContext` và chạy TRƯỚC `SeedIdentityAsync`. Giải pháp: seed document trong một bước RIÊNG sau khi identity seed xong.

**Files:**
- Modify: `src/DataAccessLayer/Data/DatabaseBootstrapper.cs` (thêm `SeedSampleDocumentsAsync`, gọi sau `SeedIdentityAsync`)

- [ ] **Step 1:** Trong `InitializeAsync`, sau `await SeedIdentityAsync(scope.ServiceProvider);` thêm:

```csharp
await SeedSampleDocumentsAsync(scope.ServiceProvider);
```

- [ ] **Step 2:** Thêm method (đặt 1-2 document tiếng Việt; id cố định để idempotent):

```csharp
private static async Task SeedSampleDocumentsAsync(IServiceProvider services)
{
    var db = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    var adminEmail = "admin@prn222.local";
    var admin = await userManager.FindByEmailAsync(adminEmail);
    var uploaderId = admin?.Id;

    var docId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    if (await db.Documents.AnyAsync(x => x.Id == docId))
    {
        return; // idempotent
    }

    var chapter2 = Prn222SeedData.Chapters[1].Id; // Chapter 02 - Async

    var content = """
    Chương 2: Lập trình bất đồng bộ và song song trong .NET

    Lập trình bất đồng bộ (asynchronous programming) cho phép chương trình thực hiện
    các tác vụ tốn thời gian như gọi mạng, đọc ghi tệp, hoặc truy vấn cơ sở dữ liệu
    mà không chặn luồng (thread) hiện tại. Trong .NET, mô hình async/await dựa trên
    kiểu Task và Task<T> giúp viết mã bất đồng bộ trông gần giống mã tuần tự.

    Từ khóa async đánh dấu một phương thức có thể chứa await. Từ khóa await tạm dừng
    việc thực thi phương thức cho đến khi Task hoàn thành, nhưng không chặn luồng gọi;
    luồng được trả về thread pool để phục vụ công việc khác. Nhờ vậy ứng dụng web
    ASP.NET Core có thể phục vụ nhiều yêu cầu đồng thời với ít luồng hơn.

    CancellationToken được dùng để hủy một tác vụ bất đồng bộ một cách hợp tác.
    Phương thức nhận CancellationToken nên kiểm tra trạng thái hủy và dừng sớm khi
    được yêu cầu, giúp giải phóng tài nguyên và tránh treo yêu cầu.

    Lập trình song song (parallel programming) khác với bất đồng bộ: nó dùng nhiều
    luồng để chạy đồng thời các phần việc nặng về CPU, ví dụ qua Parallel.For hoặc
    PLINQ. Khi nhiều luồng cùng truy cập dữ liệu chia sẻ, cần bảo đảm an toàn luồng
    (thread-safety) bằng khóa (lock) hoặc các kiểu dữ liệu đồng bộ để tránh race condition.
    """;

    db.Documents.Add(new Document
    {
        Id = docId,
        ChapterId = chapter2,
        UploadedByUserId = uploaderId,
        OriginalFileName = "chuong-02-lap-trinh-bat-dong-bo.txt",
        FileType = ".txt",
        FileSizeBytes = System.Text.Encoding.UTF8.GetByteCount(content),
        ContentText = content,
        IndexStatus = DocumentIndexStatus.Pending,
        IndexProgressPercent = 0,
        IndexStage = "Queued",
        UploadedAtUtc = DateTime.UtcNow
    });

    await db.SaveChangesAsync();
}
```

> Cần `using Microsoft.EntityFrameworkCore;` (đã có) cho `AnyAsync`. `PendingDocumentQueueHostedService` sẽ nhặt Pending lúc startup → chunk + normalize. Lexical retrieval chạy ngay cả khi không có embedding key.

- [ ] **Step 3:** Build. Verify: xoá DB, chạy app, vào `/documents` thấy tài liệu `chuong-02-...txt` chuyển sang `Indexed`; vào `/chat` hỏi "async await dùng để làm gì" → có câu trả lời + citation. **Kiểm tra encoding:** mở `Prn222SeedData`/bootstrapper trong VS Code UTF-8, ký tự tiếng Việt hiển thị đúng (không `Ã£`, `á»`).
- [ ] **Step 4:** Commit: `feat(seed): Vietnamese sample document auto-indexed on startup (C3, S9)`

## Task 2.3 — Viết lại 50 câu test set sang tiếng Việt (S2)

**Files:**
- Modify: `src/DataAccessLayer/Data/Seed/Prn222SeedData.cs` (method `BuildQuestions`, dòng 87-158)

- [ ] **Step 1:** Dịch toàn bộ 50 dòng `(Order, Chapter, Question, GroundTruth)` sang tiếng Việt, GIỮ NGUYÊN `Order`, `Chapter`, và id mapping. Mẫu 4 dòng đầu (làm theo đúng pattern cho 46 dòng còn lại):

```csharp
(1, 1, "PRN222 là môn học về gì?", "PRN222 là môn Lập trình ứng dụng đa nền tảng nâng cao với .NET. Môn tập trung vào phát triển ứng dụng web .NET với ASP.NET Core MVC, Razor Pages, Blazor, SignalR, Worker Service, EF Core, lập trình bất đồng bộ và dependency injection."),
(2, 1, "Những công cụ chính nào cần cho PRN222?", "Yêu cầu công cụ của PRN222 gồm có kết nối Internet, Visual Studio .NET 2022 trở lên, SQL Server 2019 trở lên, và .NET 8.0 trở lên."),
(3, 1, "PRN222 có mấy tín chỉ và môn tiên quyết là gì?", "PRN222 có 3 tín chỉ và môn tiên quyết là PRN212."),
(4, 1, "Các nhóm kỹ năng chính mà CLO của PRN222 bao phủ là gì?", "Các CLO bao phủ lập trình bất đồng bộ và song song, dependency injection, ASP.NET Core MVC, Razor Pages, Blazor, giao tiếp thời gian thực, và Worker Service."),
```

> Yêu cầu: dịch tự nhiên, giữ thuật ngữ kỹ thuật tiếng Anh (async/await, Task, dependency injection, middleware...). KHÔNG đổi số lượng dòng (vẫn đúng 50). Đối chiếu ground truth với nội dung tài liệu seed ở Task 2.2 cho các câu Chapter 2 (để RAGAS recall có ý nghĩa).
> **Encoding:** Sau khi sửa, mở file trong VS Code UTF-8 kiểm tra dấu tiếng Việt đúng. Lưu UTF-8 không BOM.

- [ ] **Step 2:** Build. Verify: xoá DB, chạy, vào `/benchmark` thấy 50 câu tiếng Việt.
- [ ] **Step 3:** Commit: `feat(seed): rewrite 50-question test set in Vietnamese (S2)`

---

# PHASE 3 — FUNCTIONAL / CORRECTNESS BUGS (S4, S8, N9, N7)

## Task 3.1 — Sửa prompt lặp câu hỏi (S4)

**Files:**
- Modify: `src/BusinessLayer/Services/ChatService.cs` (dòng 64-66)

- [ ] **Step 1:** Build history TRƯỚC khi lưu user message — đảo thứ tự:

```csharp
// Build history BEFORE persisting the current user message (avoids duplicating the question)
var history = await BuildHistoryAsync(sessionId, userId, cancellationToken);
await _chatRepository.AddMessageAsync(userMessage, cancellationToken);
```

- [ ] **Step 2:** Build. Verify: chat 2 lượt, lượt 2 vẫn nhớ ngữ cảnh, câu hỏi hiện tại không bị nhân đôi trong prompt (kiểm tra qua log nếu cần). Commit: `fix(chat): build history before saving current message to avoid prompt duplication (S4)`

## Task 3.2 — DOCX extraction có spacing (S8)

**Files:**
- Modify: `src/BusinessLayer/Parsing/DocumentTextExtractor.cs` (method `ExtractDocx`, dòng 51-55)

- [ ] **Step 1:** Thêm using alias đầu file:

```csharp
using Wordprocessing = DocumentFormat.OpenXml.Wordprocessing;
```

- [ ] **Step 2:** Thay `ExtractDocx`:

```csharp
private static string ExtractDocx(Stream stream)
{
    using var document = WordprocessingDocument.Open(stream, false);
    var body = document.MainDocumentPart?.Document?.Body;
    if (body is null)
    {
        return "";
    }

    var builder = new StringBuilder();
    foreach (var paragraph in body.Descendants<Wordprocessing.Paragraph>())
    {
        var text = paragraph.InnerText;
        if (!string.IsNullOrWhiteSpace(text))
        {
            builder.AppendLine(text).AppendLine();
        }
    }

    return builder.ToString();
}
```

- [ ] **Step 3:** Build. Verify: upload 1 file .docx nhiều đoạn → vào Document Details xem chunks tách đúng (không dính `Chapter 1Introduction...`). Commit: `fix(parsing): DOCX paragraph spacing instead of InnerText blob (S8)`

## Task 3.3 — GeminiClient parse phòng thủ (N9)

**Files:**
- Modify: `src/BusinessLayer/AI/GeminiClient.cs` (dòng 62-73)

- [ ] **Step 1:** Thay đoạn parse cứng:

```csharp
using var document = JsonDocument.Parse(json);
var root = document.RootElement;

if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
{
    var blockReason = root.TryGetProperty("promptFeedback", out var pf) && pf.TryGetProperty("blockReason", out var br)
        ? br.GetString() : "no candidates";
    throw new InvalidOperationException($"Gemini returned no answer ({blockReason}).");
}

var candidate = candidates[0];
if (!candidate.TryGetProperty("content", out var content)
    || !content.TryGetProperty("parts", out var parts)
    || parts.ValueKind != JsonValueKind.Array
    || parts.GetArrayLength() == 0)
{
    var finishReason = candidate.TryGetProperty("finishReason", out var fr) ? fr.GetString() : "unknown";
    throw new InvalidOperationException($"Gemini returned no content (finishReason: {finishReason}).");
}

var text = parts[0].TryGetProperty("text", out var t) ? t.GetString() : null;
if (string.IsNullOrWhiteSpace(text))
{
    throw new InvalidOperationException("Gemini did not return any content.");
}

return text.Trim();
```

- [ ] **Step 2:** Build. Commit: `fix(gemini): defensive response parsing for SAFETY/empty candidates (N9)`

## Task 3.4 — ChatHub kiểm tra ownership khi join (N7)

**Files:**
- Modify: `src/DataAccessLayer/Repositories/IChatRepository.cs` (thêm `SessionExistsAsync`)
- Modify: `src/BusinessLayer/Services/ChatService.cs` (thêm `CanAccessSessionAsync`)
- Modify: `src/PresentationLayer/Hubs/ChatHub.cs` (`JoinSession`)

- [ ] **Step 1:** `IChatRepository` interface + impl thêm:

```csharp
Task<bool> SessionExistsAsync(Guid sessionId, CancellationToken cancellationToken);
```
```csharp
public async Task<bool> SessionExistsAsync(Guid sessionId, CancellationToken cancellationToken)
{
    return await _db.ChatSessions.AnyAsync(x => x.Id == sessionId, cancellationToken);
}
```

- [ ] **Step 2:** `ChatService` thêm:

```csharp
public async Task<bool> CanAccessSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
{
    var owned = await _chatRepository.GetOwnedSessionAsync(sessionId, userId, cancellationToken);
    if (owned is not null)
    {
        return true; // user owns it
    }

    // Brand-new session id (not yet persisted) is allowed; existing-but-not-owned is denied.
    return !await _chatRepository.SessionExistsAsync(sessionId, cancellationToken);
}
```

- [ ] **Step 3:** `ChatHub.JoinSession` thêm guard:

```csharp
public async Task JoinSession(string sessionId)
{
    if (!Guid.TryParse(sessionId, out var parsed))
    {
        await Clients.Caller.SendAsync("MessageFailed", "Invalid session ID.");
        return;
    }

    if (!await _chatService.CanAccessSessionAsync(parsed, CurrentUserId(), Context.ConnectionAborted))
    {
        await Clients.Caller.SendAsync("MessageFailed", "You do not have access to this session.");
        return;
    }

    await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
}
```

- [ ] **Step 4:** Build. Verify: login 2 user khác nhau, user B không join được session của user A. Commit: `fix(security): verify session ownership before joining chat group (N7)`

---

# PHASE 4 — CHAT SESSION HISTORY + UX (S3, M1, M2, M3)

> Mục tiêu: sidebar danh sách phiên cũ, mở lại phiên, xóa phiên; loading indicator; markdown render; disable input khi Gemini thiếu.

## Task 4.1 — Backend: list/delete session

**Files:**
- Modify: `src/DataAccessLayer/Repositories/IChatRepository.cs`
- Modify: `src/BusinessLayer/Services/ChatService.cs`
- Modify: `src/BusinessLayer/DTOs/AppDtos.cs`
- Modify: `src/PresentationLayer/Controllers/ChatController.cs`

- [ ] **Step 1:** Thêm DTO vào `AppDtos.cs`:

```csharp
public record ChatSessionDto(Guid Id, string Title, Guid CourseId, DateTime UpdatedAtUtc);
```

- [ ] **Step 2:** `IChatRepository` + impl:

```csharp
Task<IReadOnlyList<ChatSession>> ListSessionsAsync(Guid userId, CancellationToken cancellationToken);
Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
```
```csharp
public async Task<IReadOnlyList<ChatSession>> ListSessionsAsync(Guid userId, CancellationToken cancellationToken)
{
    return await _db.ChatSessions
        .Where(x => x.UserId == userId)
        .OrderByDescending(x => x.UpdatedAtUtc)
        .AsNoTracking()
        .ToListAsync(cancellationToken);
}

public async Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
{
    var deleted = await _db.ChatSessions
        .Where(x => x.Id == sessionId && x.UserId == userId)
        .ExecuteDeleteAsync(cancellationToken);
    return deleted > 0;
}
```

> Kiểm tra cascade: `ChatMessage` FK tới `ChatSession` cần `OnDelete(Cascade)` để xóa session kéo theo messages. Đọc `AppDbContext.cs` cấu hình `ChatMessage`/`ChatSession`. Nếu CHƯA cascade → xóa messages thủ công trước trong `DeleteSessionAsync` (thêm `ExecuteDeleteAsync` trên `ChatMessages` theo `ChatSessionId`) thay vì đổi migration.

- [ ] **Step 3:** `ChatService`:

```csharp
public async Task<IReadOnlyList<ChatSessionDto>> ListSessionsAsync(Guid userId, CancellationToken cancellationToken)
{
    var sessions = await _chatRepository.ListSessionsAsync(userId, cancellationToken);
    return sessions.Select(x => new ChatSessionDto(x.Id, x.Title, x.CourseId, x.UpdatedAtUtc)).ToList();
}

public Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    => _chatRepository.DeleteSessionAsync(sessionId, userId, cancellationToken);
```

- [ ] **Step 4:** `ChatController` thêm 2 endpoint:

```csharp
[HttpGet("/api/chat/sessions")]
public async Task<IActionResult> Sessions(CancellationToken cancellationToken)
    => Json(new { success = true, sessions = await _chatService.ListSessionsAsync(CurrentUserId(), cancellationToken) });

[HttpDelete("/api/chat/{sessionId:guid}")]
public async Task<IActionResult> DeleteSession(Guid sessionId, CancellationToken cancellationToken)
    => Json(new { success = await _chatService.DeleteSessionAsync(sessionId, CurrentUserId(), cancellationToken) });
```

- [ ] **Step 5:** Build → 0 errors. Commit: `feat(chat): backend list/delete chat sessions (S3)`

## Task 4.2 — UI: sidebar phiên + disable + loading + markdown

**Files:**
- Modify: `src/PresentationLayer/Views/Chat/Index.cshtml`
- Modify: `src/PresentationLayer/wwwroot/js/chat.js`
- Modify: `src/PresentationLayer/wwwroot/css/site.css` (style nhẹ cho session list + typing dots)

- [ ] **Step 1 (cshtml — M3 disable + sidebar phiên):** Trong `.chat-sidebar`, thêm khối danh sách phiên trên card Settings:

```html
<div class="card app-card mb-3">
    <div class="card-body">
        <div class="d-flex justify-content-between align-items-center mb-2">
            <h6 class="card-title mb-0">Phiên trò chuyện</h6>
            <a href="/chat" class="btn btn-sm btn-outline-primary">+ Mới</a>
        </div>
        <ul id="sessionList" class="list-unstyled small mb-0"></ul>
    </div>
</div>
```
Và đặt `disabled` cho input/Send khi Gemini thiếu — sửa form:
```html
<form id="chatForm" class="chat-form">
    <input id="messageInput" class="form-control" autocomplete="off"
           placeholder="Đặt câu hỏi về môn học..."
           @(Model.GeminiConfigured ? "" : "disabled") />
    <button class="btn btn-primary" type="submit" @(Model.GeminiConfigured ? "" : "disabled")>Gửi</button>
</form>
```

- [ ] **Step 2 (chat.js — M1 loading + M2 markdown + session list):** Bổ sung:
  - Hàm `renderMarkdown(text)` tự viết (KHÔNG thêm lib), escape HTML trước rồi áp dụng: `**bold**`, `` `code` ``, dòng `- ` thành list, `\n` thành `<br>`. Dùng cho `body.innerHTML` thay `body.textContent` ở dòng 23.
  - Typing indicator: trước khi `connection.invoke("SendMessage", ...)` append 1 phần tử `.message.assistant.typing` với 3 chấm; trong `connection.on("MessageReceived", ...)` nếu message là assistant thì gỡ phần tử typing trước khi render.
  - `loadSessions()`: `fetch('/api/chat/sessions')` → render `<li>` có link `/chat?sessionId=<id>` + nút xóa gọi `DELETE /api/chat/<id>` rồi `loadSessions()`.
  - Gọi `loadSessions()` trong chuỗi `connection.start().then(...)`.

Mẫu `renderMarkdown` (an toàn XSS, escape trước):
```javascript
function escapeHtml(s) {
  return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}
function renderMarkdown(text) {
  let html = escapeHtml(text);
  html = html.replace(/`([^`]+)`/g, "<code>$1</code>");
  html = html.replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>");
  html = html.replace(/^\s*-\s+(.*)$/gm, "<li>$1</li>");
  html = html.replace(/(<li>[\s\S]*?<\/li>)/g, "<ul>$1</ul>");
  html = html.replace(/\n/g, "<br>");
  return html;
}
```
Đổi dòng render body: `body.innerHTML = renderMarkdown(message.content);`

- [ ] **Step 3 (css):** Thêm `.typing` 3-dot animation + style `#sessionList li` (hover, active). Style tối giản, theo tông Bootstrap sẵn có.

- [ ] **Step 4:** Build + chạy. Verify: (a) gửi tin → thấy typing dots → câu trả lời render đậm/nghiêng/list; (b) sidebar liệt kê phiên, bấm mở lại đúng phiên; (c) xóa phiên hoạt động; (d) khi chưa set Gemini key → input/Send bị disable.
- [ ] **Step 5:** Commit: `feat(chat): session sidebar, loading indicator, markdown render, disable when Gemini missing (S3, M1, M2, M3)`

---

# PHASE 5 — BENCHMARK UX + BACKGROUND JOB (S5, D7, D4, D8)

> Full benchmark (đến 600 eval) KHÔNG chạy đồng bộ trong 1 HTTP request → timeout. Chuyển sang background + polling tiến độ.

## Task 5.1 — Background benchmark runner

**Files:**
- Create: `src/BusinessLayer/Services/BenchmarkJobRunner.cs` (singleton giữ trạng thái job: total/done/status, chạy nền)
- Modify: `src/PresentationLayer/Program.cs` (đăng ký singleton + `IHttpClientFactory` đã có)
- Modify: `src/PresentationLayer/Controllers/BenchmarkController.cs`

- [ ] **Step 1:** `BenchmarkJobRunner` (singleton) phơi: `Start(limit)` → tạo scope nội bộ resolve `EvaluationService`, chạy `Task.Run` cập nhật `Progress { Total, Done, Running, Error }`; `GetProgress()`. Dùng `IServiceScopeFactory` để resolve scoped `EvaluationService` trong job nền. Bảo vệ chạy chồng (nếu `Running` → trả lỗi "already running") → fix D8 (đồng thời) + D7 (double-click).

- [ ] **Step 2:** `BenchmarkController`:
  - `POST /api/evaluations/run-full` → gọi `_jobRunner.Start(limit)` trả ngay `{ started: true }` (không chờ). Nếu đang chạy → `409`.
  - `GET /api/evaluations/progress` → `_jobRunner.GetProgress()`.
  - Thêm `POST /api/evaluations/clear` → xóa toàn bộ `EvaluationResults` (thêm `IEvaluationRepository.ClearResultsAsync` dùng `ExecuteDeleteAsync`). Fix "không xóa được kết quả cũ" + N8.
  - Thêm `[ValidateAntiForgeryToken]` cho các POST + truyền token từ view (fix D4).

- [ ] **Step 3:** Build. Commit: `feat(benchmark): background job runner with progress + clear results (S5, D7, D8)`

## Task 5.2 — benchmark.js progress polling

**Files:**
- Modify: `src/PresentationLayer/wwwroot/js/benchmark.js`
- Modify: `src/PresentationLayer/Views/Benchmark/Index.cshtml` (progress bar, nút Clear, antiforgery token)

- [ ] **Step 1:** Sau khi POST run-full thành công, mở `setInterval` poll `/api/evaluations/progress` mỗi 2s → cập nhật progress bar `Done/Total`; khi `Running=false` → reload results + vẽ lại chart. Disable nút Run khi đang chạy (fix double-click). Thay `prompt()` nhập số bằng `<input type="number">` trong view.
- [ ] **Step 2:** Nút "Xóa kết quả" gọi `/api/evaluations/clear`.
- [ ] **Step 3:** Build + chạy benchmark thật (limit 2). Verify progress bar tăng, không timeout, chart cập nhật. Commit: `feat(benchmark): progress UI, numeric input, clear button (D7)`

---

# PHASE 6 — ACCOUNT / ADMIN (M6, M7, M8, M9)

> Tất cả dùng `UserManager`/Identity sẵn có. KHÔNG đổi schema.

## Task 6.1 — Đổi mật khẩu (M6)

- [ ] `AccountController`: thêm `GET/POST /account/change-password` dùng `UserManager.ChangePasswordAsync`. ViewModel `ChangePasswordViewModel` (Current/New/Confirm). View `Account/ChangePassword.cshtml`. Link trong `_Layout` user menu. Commit.

## Task 6.2 — Đăng ký tài khoản (M7) — **ĐÃ CHỐT: BỎ QUA**

> **Decision (2026-06-26):** Chỉ Admin tạo user qua `/admin/users/create`. Self-register không cần thiết cho môi trường học thuật. Task này **SKIP**.

## Task 6.3 — Admin xóa user + reset password (M8, M9)

- [ ] `UserAdminService`: thêm `DeleteAsync(userId)` (`UserManager.DeleteAsync`, chặn tự xóa chính mình/last admin), `ResetPasswordAsync(userId, newPassword)` (`GeneratePasswordResetTokenAsync` + `ResetPasswordAsync`).
- [ ] `AdminUsersController` + view `AdminUsers/Index.cshtml`: nút Delete + Reset Password (modal/inline). Commit.

---

# PHASE 7 — PERFORMANCE (S10, S11, S12, M5)

## Task 7.1 — UserAdminService bỏ N+1 (S10)

- [ ] `src/BusinessLayer/Services/UserAdminService.cs`: thay vòng lặp `GetRolesAsync` từng user bằng truy vấn batch qua `AppDbContext` join `UserRoles`/`Roles` (1 query). Map role về `UserListDto`. Build + verify trang `/admin/users` vẫn đúng role. Commit.

## Task 7.2 — Projection cho list documents (S12, M5)

- [ ] `IDocumentRepository.ListWithChapterAndChunksAsync`: tạo overload/biến thể cho TRANG DANH SÁCH dùng `.Select(...)` projection KHÔNG kéo `ContentText`, chỉ lấy field cần + `Chunks.Count`. `DocumentService.GetIndexDataAsync` dùng biến thể này.
- [ ] LƯU Ý: `BenchmarkRetrievalService` (Task 1.6) VẪN cần `ContentText` → giữ method gốc cho path benchmark, projection chỉ cho list UI. Đừng phá benchmark.
- [ ] (M5 pagination) thêm tham số `page/pageSize` + `Skip/Take`. Build. Commit.

## Task 7.3 — Lexical retrieval không load toàn bộ (S11)

- [ ] `ListIndexedChunksAsync`: thêm lọc sơ bộ theo từ khóa ở SQL (`Where(NormalizedContent.Contains(term))` cho term dài nhất) để giảm tập nạp vào memory, hoặc giới hạn `Take(N)`. Đây là tối ưu "đủ tốt" cho demo; ghi chú giới hạn. Build. Commit.

---

# PHASE 8 — DOCS, CHECKLIST, POLISH (S7, S13, D5, D6, L1–L5, N6)

## Task 8.1 — Sửa checklist.md cho trung thực (S7)

- [ ] `checklist.md`: cập nhật trạng thái thực: benchmark chunking/embedding giờ đã hoạt động THẬT sau Phase 1 (đánh ✅ kèm ghi chú cần có API key embedding để so sánh đầy đủ; không key → một số model `Skipped`). KHÔNG để "100%" nếu còn hạng mục chưa làm. Commit.

## Task 8.2 — Điền research_report.md (D6)

- [ ] Sau khi chạy benchmark thật (Phase 1+2 xong, có ≥1 embedding key): export kết quả `/api/evaluations/export`, điền bảng RAGAS (Faithfulness/AnswerRelevance/RetrievalRecall/CitationAccuracy theo strategy × model), bảng latency RAG vs fine-tune (nếu có FT endpoint), và mục Hạn chế (self-scoring bias D1, HF inference rate limit, FT cần endpoint ngoài). Commit.

## Task 8.3 — global.json + README + Architecture diagram (S13, D5, README)

- [ ] `global.json`: hoặc hạ SDK pin về dòng khớp `net8.0`, hoặc thêm note rõ `rollForward`. Thống nhất với user.
- [ ] `README.md`: hướng dẫn setup đầy đủ — user-secrets cho `Gemini:ApiKey`, `HuggingFace:ApiKey` (OpenAI không dùng); default login (`admin@prn222.local` / `Prn222@123`); cách chạy benchmark; giải thích RAG vs fine-tune. Ghi chú: FT endpoint để trống (`FineTune:EndpointUrl = ""`), phần so sánh FT để triển khai sau khi có model fine-tuned.
- [ ] `Architecture/Index.cshtml`: thêm sơ đồ Mermaid 3-layer + luồng RAG/benchmark (D5).
- [ ] Commit.

## Task 8.4 — Polish L1–L5 + N6 (gộp, nhỏ)

- [ ] L1 empty states, L2 breadcrumbs, L3 toast (thay `alert`/`TempData`), L4 mobile chat responsive, L5 SignalR error UX (thay `alert` bằng toast/inline). N6: ghi chú README rằng Gemini key nằm trong query string (giới hạn provider, không đổi được). Commit theo nhóm nhỏ.

---

# VERIFICATION TỔNG THỂ (chạy sau mỗi phase lớn)

1. `dotnet build Prn222Chatbot.sln` → **0 errors, 0 warnings mới**.
2. Xoá DB `Prn222RagChatbot` (SSMS hoặc `sqllocaldb`), `dotnet run --project src/PresentationLayer/PresentationLayer.csproj`.
3. **Fresh install:** login `admin@prn222.local` / `Prn222@123` được ngay (không cần secrets).
4. **Demo RAG:** `/documents` thấy tài liệu tiếng Việt `Indexed`; `/chat` hỏi tiếng Việt → trả lời + citation; lịch sử phiên hiện ở sidebar, mở lại được.
5. **Research (cần ≥1 embedding key):** set `HuggingFace:ApiKey` + `Gemini:ApiKey` qua user-secrets; re-index tài liệu (nút Retry hoặc upload lại); `/benchmark` run-full limit 2 → progress bar chạy, kết quả KHÁC NHAU giữa strategy/model; model thiếu key hiện `Skipped` (không giả).
6. **Encoding:** mở các file seed/cshtml tiếng Việt trong VS Code UTF-8 → không mojibake; `git diff` không nhiễu BOM.

---

# DECISIONS ĐÃ CHỐT (2026-06-26)

| # | Quyết định | Kết quả |
|---|---|---|
| 1 | M7 self-register | **SKIP** — chỉ Admin tạo user qua `/admin/users/create` |
| 2 | Fine-tune endpoint | **Để trống** — `FineTune:EndpointUrl = ""`, cột FT hiện `N/A`, ghi chú triển khai sau trong research report |
| 3 | OpenAI key | **Không có** — xóa `text-embedding-3-small` khỏi `appsettings.json`; chỉ benchmark 3 HF models: `multilingual-e5-base`, `phobert-base`, `bge-m3` |
| 4 | Thứ tự | **Đồng ý**: Phase 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 |

---

# THỨ TỰ THỰC THI KHUYẾN NGHỊ

```
Phase 1 (research validity)  ──> đây là phần CỨU đề tài, làm trước
Phase 2 (demo data VN)       ──> để verify được Phase 1 và đúng "tiếng Việt"
Phase 3 (bug logic)          ──> rẻ, tác động cao
Phase 4 (chat history + UX)  ──> deliverable web app
Phase 5 (benchmark UX/job)   ──> tránh timeout khi chạy thật
Phase 6-7 (account/perf)     ──> hoàn thiện
Phase 8 (docs/report/polish) ──> deliverable nghiên cứu, làm CUỐI sau khi có số liệu thật
```
