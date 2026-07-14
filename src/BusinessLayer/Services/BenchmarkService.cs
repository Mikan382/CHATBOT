using System.Diagnostics;
using System.Text.Json;
using BusinessLayer.AI;
using BusinessLayer.DTOs;
using BusinessLayer.Indexing;
using BusinessLayer.Retrieval;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Services;

public class BenchmarkService : IBenchmarkService
{
    private const int MaxQuestionsPerRun = 50;
    private const int MaxBenchmarkChunks = 500;
    private const int MaxSearchLength = 100;
    private readonly IBenchmarkRepository _benchmarkRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IGeminiClient _geminiClient;
    private readonly IReadOnlyDictionary<string, ITextChunker> _chunkers;

    public BenchmarkService(
        IBenchmarkRepository benchmarkRepository,
        ICourseRepository courseRepository,
        IEmbeddingClient embeddingClient,
        IGeminiClient geminiClient,
        IEnumerable<ITextChunker> chunkers)
    {
        _benchmarkRepository = benchmarkRepository;
        _courseRepository = courseRepository;
        _embeddingClient = embeddingClient;
        _geminiClient = geminiClient;
        _chunkers = chunkers
            .GroupBy(x => x.StrategyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
    }

    public async Task<BenchmarkDashboardDto> GetDashboardAsync(Guid? courseId, CancellationToken cancellationToken)
    {
        var courses = await GetCourseOptionsAsync(cancellationToken);
        var selectedCourseId = ResolveCourseId(courseId, courses);
        var questions = await _benchmarkRepository.ListQuestionsAsync(selectedCourseId, null, null, cancellationToken);
        var runs = await _benchmarkRepository.ListRunsAsync(selectedCourseId, 100, cancellationToken);
        var summaries = runs.Select(ToRunSummary).ToList();
        var latestComparisons = summaries
            .GroupBy(x => new { x.ChunkingStrategy, x.EmbeddingModelName })
            .Select(group => group.OrderByDescending(x => x.CompletedAtUtc).First())
            .OrderBy(x => x.ChunkingStrategy)
            .ThenBy(x => x.EmbeddingModelName)
            .ToList();

        return new BenchmarkDashboardDto(
            courses,
            selectedCourseId,
            questions.Count,
            questions.Count(x => x.IsActive),
            _chunkers.Keys.OrderBy(x => x).ToList(),
            _embeddingClient.AvailableModels,
            _geminiClient.IsConfigured,
            _embeddingClient.AvailableModels.Any(_embeddingClient.IsModelConfigured),
            latestComparisons,
            summaries.Take(20).ToList());
    }

    public async Task<BenchmarkQuestionPageDto> ListQuestionsAsync(
        Guid? courseId,
        string? searchTerm,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = searchTerm?.Trim();
        if (normalizedSearch?.Length > MaxSearchLength)
        {
            throw new InvalidOperationException($"Search cannot exceed {MaxSearchLength} characters.");
        }

        var courses = await GetCourseOptionsAsync(cancellationToken);
        var questions = await _benchmarkRepository.ListQuestionsAsync(courseId, normalizedSearch, isActive, cancellationToken);
        return new BenchmarkQuestionPageDto(
            courses,
            questions.Select(ToQuestionDto).ToList(),
            courseId,
            normalizedSearch,
            isActive);
    }

    public async Task<BenchmarkQuestionEditorDto> GetCreateQuestionAsync(
        Guid? courseId,
        CancellationToken cancellationToken)
    {
        var courses = await GetCourseOptionsAsync(cancellationToken);
        var selectedCourseId = ResolveCourseId(courseId, courses);
        var documents = await GetDocumentOptionsAsync(cancellationToken);
        var questions = selectedCourseId.HasValue
            ? await _benchmarkRepository.ListQuestionsAsync(selectedCourseId, null, null, cancellationToken)
            : [];

        return new BenchmarkQuestionEditorDto(
            null,
            courses,
            documents,
            selectedCourseId,
            questions.Count == 0 ? 1 : questions.Max(x => x.DisplayOrder) + 1);
    }

    public async Task<BenchmarkQuestionEditorDto?> GetEditQuestionAsync(Guid id, CancellationToken cancellationToken)
    {
        var question = await _benchmarkRepository.GetQuestionAsync(id, cancellationToken);
        if (question is null)
        {
            return null;
        }

        return new BenchmarkQuestionEditorDto(
            ToQuestionDto(question),
            await GetCourseOptionsAsync(cancellationToken),
            await GetDocumentOptionsAsync(cancellationToken),
            question.CourseId,
            question.DisplayOrder);
    }

    public async Task CreateQuestionAsync(
        Guid courseId,
        Guid expectedDocumentId,
        string question,
        string expectedAnswer,
        int displayOrder,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var input = await ValidateQuestionInputAsync(
            courseId,
            expectedDocumentId,
            question,
            expectedAnswer,
            displayOrder,
            cancellationToken);
        var now = DateTime.UtcNow;

        await _benchmarkRepository.AddQuestionAsync(new EvaluationQuestion
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            ExpectedDocumentId = expectedDocumentId,
            ExpectedSourceName = input.ExpectedSourceName,
            Question = input.Question,
            ExpectedAnswer = input.ExpectedAnswer,
            DisplayOrder = displayOrder,
            IsActive = isActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);
    }

    public async Task UpdateQuestionAsync(
        Guid id,
        Guid courseId,
        Guid expectedDocumentId,
        string question,
        string expectedAnswer,
        int displayOrder,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var entity = await _benchmarkRepository.GetQuestionAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Evaluation question was not found.");
        var input = await ValidateQuestionInputAsync(
            courseId,
            expectedDocumentId,
            question,
            expectedAnswer,
            displayOrder,
            cancellationToken);

        entity.CourseId = courseId;
        entity.ExpectedDocumentId = expectedDocumentId;
        entity.ExpectedSourceName = input.ExpectedSourceName;
        entity.Question = input.Question;
        entity.ExpectedAnswer = input.ExpectedAnswer;
        entity.DisplayOrder = displayOrder;
        entity.IsActive = isActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _benchmarkRepository.SaveQuestionAsync(cancellationToken);
    }

    public async Task DeleteQuestionAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await _benchmarkRepository.DeleteQuestionAsync(id, cancellationToken))
        {
            throw new InvalidOperationException("Evaluation question was not found.");
        }
    }

    public async Task<Guid> RunAsync(
        Guid courseId,
        string chunkingStrategy,
        string embeddingModel,
        int topK,
        CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");
        if (!_chunkers.TryGetValue(chunkingStrategy?.Trim() ?? "", out var chunker))
        {
            throw new InvalidOperationException("Chunking strategy is invalid.");
        }

        var normalizedModel = embeddingModel?.Trim() ?? "";
        if (!_embeddingClient.AvailableModels.Contains(normalizedModel, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Embedding model is invalid.");
        }

        if (!_embeddingClient.IsModelConfigured(normalizedModel))
        {
            throw new InvalidOperationException($"Embedding model '{normalizedModel}' or the Hugging Face API key is not configured.");
        }

        if (!_geminiClient.IsConfigured)
        {
            throw new InvalidOperationException("Gemini API key is required to evaluate generated answers.");
        }

        if (topK is < 1 or > 10)
        {
            throw new InvalidOperationException("Top K must be between 1 and 10.");
        }

        var questions = await _benchmarkRepository.ListActiveQuestionsAsync(courseId, cancellationToken);
        if (questions.Count == 0)
        {
            throw new InvalidOperationException("Add at least one active ground-truth question before running a benchmark.");
        }

        if (questions.Count > MaxQuestionsPerRun)
        {
            throw new InvalidOperationException($"A benchmark run supports at most {MaxQuestionsPerRun} active questions.");
        }

        var documents = (await _benchmarkRepository.ListDocumentsAsync(courseId, cancellationToken))
            .Where(x => !string.IsNullOrWhiteSpace(x.ContentText))
            .ToList();
        if (documents.Count == 0)
        {
            throw new InvalidOperationException("The selected course does not have any indexed document content.");
        }

        var documentIds = documents.Select(x => x.Id).ToHashSet();
        var invalidQuestion = questions.FirstOrDefault(x => !documentIds.Contains(x.ExpectedDocumentId));
        if (invalidQuestion is not null)
        {
            throw new InvalidOperationException(
                $"Question #{invalidQuestion.DisplayOrder} references a document that is no longer available.");
        }

        var totalStopwatch = Stopwatch.StartNew();
        var corpus = await BuildCorpusAsync(documents, chunker, normalizedModel, cancellationToken);
        var run = new BenchmarkRun
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            CourseCode = course.Code,
            CourseName = course.Name,
            ChunkingStrategy = chunker.StrategyName,
            EmbeddingModelName = normalizedModel,
            TopK = topK,
            QuestionCount = questions.Count,
            ChunkCount = corpus.Count
        };

        foreach (var question in questions)
        {
            run.Results.Add(await EvaluateQuestionAsync(
                run.Id,
                course.Name,
                question,
                corpus,
                normalizedModel,
                topK,
                cancellationToken));
        }

        totalStopwatch.Stop();
        run.DurationMilliseconds = totalStopwatch.ElapsedMilliseconds;
        run.CompletedAtUtc = DateTime.UtcNow;
        await _benchmarkRepository.AddRunAsync(run, cancellationToken);
        return run.Id;
    }

    public async Task<BenchmarkRunDetailsDto?> GetRunAsync(Guid id, CancellationToken cancellationToken)
    {
        var run = await _benchmarkRepository.GetRunAsync(id, cancellationToken);
        if (run is null)
        {
            return null;
        }

        var results = run.Results
            .OrderBy(x => x.DisplayOrder)
            .Select(result => new BenchmarkResultDetailDto(
                result.Id,
                result.DisplayOrder,
                result.Question,
                result.ExpectedAnswer,
                result.ExpectedSourceName,
                result.GeneratedAnswer,
                DeserializeSources(result.RetrievedSourcesJson),
                result.HitAtK,
                result.ReciprocalRank,
                result.AnswerTokenF1,
                result.LatencyMilliseconds))
            .ToList();

        return new BenchmarkRunDetailsDto(ToRunSummary(run), run.CourseName, results);
    }

    private async Task<IReadOnlyList<BenchmarkCorpusChunk>> BuildCorpusAsync(
        IReadOnlyList<Document> documents,
        ITextChunker chunker,
        string embeddingModel,
        CancellationToken cancellationToken)
    {
        var corpus = new List<BenchmarkCorpusChunk>();
        foreach (var document in documents)
        {
            var sections = chunker.Chunk(document.ContentText);
            if (sections.Count > DocumentUploadLimits.MaxChunksPerDocument)
            {
                throw new InvalidOperationException(
                    $"'{document.OriginalFileName}' creates more than {DocumentUploadLimits.MaxChunksPerDocument} chunks with '{chunker.StrategyName}'.");
            }

            for (var index = 0; index < sections.Count; index++)
            {
                if (corpus.Count >= MaxBenchmarkChunks)
                {
                    throw new InvalidOperationException(
                        $"The selected corpus exceeds the benchmark limit of {MaxBenchmarkChunks} chunks.");
                }

                var section = sections[index];
                var vector = await _embeddingClient.EmbedPassageAsync(
                    embeddingModel,
                    section,
                    cancellationToken);
                corpus.Add(new BenchmarkCorpusChunk(
                    Guid.NewGuid(),
                    document.Id,
                    document.OriginalFileName,
                    document.Chapter?.Title ?? "Unknown",
                    index + 1,
                    section,
                    vector));
            }
        }

        if (corpus.Count == 0)
        {
            throw new InvalidOperationException("The selected strategy did not create any benchmark chunks.");
        }

        return corpus;
    }

    private async Task<BenchmarkResult> EvaluateQuestionAsync(
        Guid runId,
        string courseName,
        EvaluationQuestion question,
        IReadOnlyList<BenchmarkCorpusChunk> corpus,
        string embeddingModel,
        int topK,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var queryVector = await _embeddingClient.EmbedQueryAsync(
            embeddingModel,
            question.Question,
            cancellationToken);
        var ranked = corpus
            .Select(chunk => new RankedBenchmarkChunk(
                chunk,
                CosineSimilarity.Cosine(queryVector.AsSpan(), chunk.Vector.AsSpan())))
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();
        var correctRank = ranked.FindIndex(x => x.Chunk.DocumentId == question.ExpectedDocumentId) + 1;
        var retrievedChunks = ranked.Select(x => new RetrievedChunkDto(
            x.Chunk.Id,
            x.Chunk.DocumentId,
            x.Chunk.SourceName,
            x.Chunk.ChapterTitle,
            x.Chunk.ChunkIndex,
            x.Chunk.Content,
            x.Score)).ToList();
        var generatedAnswer = await _geminiClient.GenerateAsync(
            RagPromptBuilder.BuildSystemInstruction(courseName),
            RagPromptBuilder.BuildPrompt(question.Question, retrievedChunks, []),
            cancellationToken);
        stopwatch.Stop();

        var sources = ranked.Select((item, index) => new BenchmarkRetrievedSourceDto(
            index + 1,
            item.Chunk.DocumentId,
            item.Chunk.SourceName,
            item.Chunk.ChapterTitle,
            item.Chunk.ChunkIndex,
            item.Score)).ToList();

        return new BenchmarkResult
        {
            Id = Guid.NewGuid(),
            BenchmarkRunId = runId,
            EvaluationQuestionId = question.Id,
            DisplayOrder = question.DisplayOrder,
            Question = question.Question,
            ExpectedAnswer = question.ExpectedAnswer,
            ExpectedDocumentId = question.ExpectedDocumentId,
            ExpectedSourceName = question.ExpectedSourceName,
            GeneratedAnswer = generatedAnswer,
            RetrievedSourcesJson = JsonSerializer.Serialize(sources),
            HitAtK = correctRank > 0,
            ReciprocalRank = correctRank > 0 ? 1d / correctRank : 0,
            AnswerTokenF1 = CalculateTokenF1(question.ExpectedAnswer, generatedAnswer),
            LatencyMilliseconds = stopwatch.ElapsedMilliseconds
        };
    }

    private async Task<QuestionInput> ValidateQuestionInputAsync(
        Guid courseId,
        Guid expectedDocumentId,
        string question,
        string expectedAnswer,
        int displayOrder,
        CancellationToken cancellationToken)
    {
        _ = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");
        var document = (await _benchmarkRepository.ListDocumentsAsync(courseId, cancellationToken))
            .FirstOrDefault(x => x.Id == expectedDocumentId)
            ?? throw new InvalidOperationException("Expected source document was not found in the selected course.");
        if (displayOrder is < 1 or > 10000)
        {
            throw new InvalidOperationException("Display order must be between 1 and 10000.");
        }

        return new QuestionInput(
            NormalizeQuestion(question),
            NormalizeExpectedAnswer(expectedAnswer),
            $"{document.OriginalFileName} - {document.Chapter?.Title ?? "Unknown"}");
    }

    private async Task<IReadOnlyList<BenchmarkCourseOptionDto>> GetCourseOptionsAsync(CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.ListAsync(null, null, cancellationToken);
        return courses.Select(x => new BenchmarkCourseOptionDto(x.Id, x.Code, x.Name)).ToList();
    }

    private async Task<IReadOnlyList<BenchmarkDocumentOptionDto>> GetDocumentOptionsAsync(CancellationToken cancellationToken)
    {
        var documents = await _benchmarkRepository.ListDocumentsAsync(null, cancellationToken);
        return documents.Select(x => new BenchmarkDocumentOptionDto(
            x.Id,
            x.Chapter!.CourseId,
            x.Chapter.Course?.Code ?? "Unknown",
            x.Chapter.Title,
            x.OriginalFileName)).ToList();
    }

    private static Guid? ResolveCourseId(Guid? requestedCourseId, IReadOnlyList<BenchmarkCourseOptionDto> courses)
    {
        if (requestedCourseId.HasValue && courses.Any(x => x.Id == requestedCourseId.Value))
        {
            return requestedCourseId;
        }

        return courses.FirstOrDefault()?.Id;
    }

    private static BenchmarkQuestionDto ToQuestionDto(EvaluationQuestion question)
    {
        return new BenchmarkQuestionDto(
            question.Id,
            question.CourseId,
            question.Course?.Code ?? "Unknown",
            question.ExpectedDocumentId,
            question.ExpectedSourceName,
            question.Question,
            question.ExpectedAnswer,
            question.DisplayOrder,
            question.IsActive);
    }

    private static BenchmarkRunSummaryDto ToRunSummary(BenchmarkRun run)
    {
        var results = run.Results.ToList();
        return new BenchmarkRunSummaryDto(
            run.Id,
            run.CourseId,
            run.CourseCode,
            run.ChunkingStrategy,
            run.EmbeddingModelName,
            run.TopK,
            run.QuestionCount,
            run.ChunkCount,
            results.Count == 0 ? 0 : results.Average(x => x.HitAtK ? 1d : 0d),
            results.Count == 0 ? 0 : results.Average(x => x.ReciprocalRank),
            results.Count == 0 ? 0 : results.Average(x => x.AnswerTokenF1),
            results.Count == 0 ? 0 : results.Average(x => x.LatencyMilliseconds),
            run.DurationMilliseconds,
            run.CompletedAtUtc);
    }

    private static IReadOnlyList<BenchmarkRetrievedSourceDto> DeserializeSources(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<BenchmarkRetrievedSourceDto>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static double CalculateTokenF1(string expectedAnswer, string generatedAnswer)
    {
        var actualWithoutCitations = string.Join('\n', generatedAnswer
            .Split('\n')
            .Where(line => !line.TrimStart().StartsWith("[Source:", StringComparison.OrdinalIgnoreCase)));
        var expectedTerms = TextNormalizer.Terms(expectedAnswer);
        var actualTerms = TextNormalizer.Terms(actualWithoutCitations);
        if (expectedTerms.Count == 0 || actualTerms.Count == 0)
        {
            return 0;
        }

        var expectedCounts = expectedTerms.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
        var actualCounts = actualTerms.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
        var matches = expectedCounts.Sum(item =>
            actualCounts.TryGetValue(item.Key, out var actualCount) ? Math.Min(item.Value, actualCount) : 0);
        var precision = matches / (double)actualTerms.Count;
        var recall = matches / (double)expectedTerms.Count;
        return precision + recall <= 0 ? 0 : 2 * precision * recall / (precision + recall);
    }

    private static string NormalizeQuestion(string value)
    {
        var normalized = string.Join(' ', (value ?? "").Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Question is required.");
        }

        if (normalized.Length > 2000)
        {
            throw new InvalidOperationException("Question cannot exceed 2000 characters.");
        }

        return normalized;
    }

    private static string NormalizeExpectedAnswer(string value)
    {
        var normalized = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Expected answer is required.");
        }

        if (normalized.Length > 8000)
        {
            throw new InvalidOperationException("Expected answer cannot exceed 8000 characters.");
        }

        return normalized;
    }

    private sealed record QuestionInput(string Question, string ExpectedAnswer, string ExpectedSourceName);

    private sealed record BenchmarkCorpusChunk(
        Guid Id,
        Guid DocumentId,
        string SourceName,
        string ChapterTitle,
        int ChunkIndex,
        string Content,
        float[] Vector);

    private sealed record RankedBenchmarkChunk(BenchmarkCorpusChunk Chunk, double Score);
}
