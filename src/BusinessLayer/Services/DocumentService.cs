using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;
using BusinessLayer.Indexing;
using BusinessLayer.Parsing;

namespace BusinessLayer.Services;

public class DocumentService
{
    private const long MaxFileSize = 20 * 1024 * 1024;
    private readonly IChapterRepository _chapterRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentTextExtractor _extractor;
    private readonly IIndexingQueue _queue;

    public DocumentService(
        IChapterRepository chapterRepository,
        ICourseRepository courseRepository,
        IDocumentRepository documentRepository,
        IDocumentTextExtractor extractor,
        IIndexingQueue queue)
    {
        _chapterRepository = chapterRepository;
        _courseRepository = courseRepository;
        _documentRepository = documentRepository;
        _extractor = extractor;
        _queue = queue;
    }

    public async Task<(IReadOnlyList<Chapter> Chapters, IReadOnlyList<Document> Documents)> GetIndexDataAsync(
        string? searchTerm,
        Guid? courseId,
        Guid? chapterId,
        DocumentIndexStatus? status,
        CancellationToken cancellationToken)
    {
        var chapters = await _chapterRepository.ListOrderedAsync(cancellationToken);
        var documents = await _documentRepository.ListWithChapterAndChunksAsync(searchTerm, courseId, chapterId, status, cancellationToken);
        return (chapters, documents);
    }

    public async Task<IReadOnlyList<CourseDto>> ListCoursesAsync(CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.ListWithChaptersAsync(cancellationToken);
        return courses.Select(x => new CourseDto(
            x.Id,
            x.Code,
            x.Name,
            x.Description,
            x.Tools,
            x.Chapters
                .OrderBy(c => c.Order)
                .Select(c => new ChapterDto(c.Id, c.Order, c.Clo, c.Title, c.Summary))
                .ToList())).ToList();
    }

    public async Task<IReadOnlyList<DocumentApiDto>> ListDocumentsAsync(CancellationToken cancellationToken)
    {
        var documents = await _documentRepository.ListWithChapterAndChunksAsync(null, null, null, null, cancellationToken);
        return documents.Select(x => new DocumentApiDto(
            x.Id,
            x.OriginalFileName,
            x.FileType,
            x.FileSizeBytes,
            x.IndexStatus.ToString(),
            x.IndexProgressPercent,
            x.IndexStage,
            x.IndexError,
            x.UploadedAtUtc,
            x.Chapter is null ? null : new ChapterDto(x.Chapter.Id, x.Chapter.Order, x.Chapter.Clo, x.Chapter.Title, x.Chapter.Summary),
            x.ChunksCount > 0 ? x.ChunksCount : x.Chunks.Count)).ToList();
    }

    public async Task<Document> GetDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _documentRepository.GetDetailsAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Document was not found.");
    }

    public async Task<IReadOnlyList<DocumentChunkApiDto>> ListChunksAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var chunks = await _documentRepository.ListChunksAsync(documentId, cancellationToken);
        return chunks.Select(x => new DocumentChunkApiDto(
            x.Id,
            x.DocumentId,
            x.ChunkIndex,
            x.SourceName,
            x.Content,
            x.CreatedAtUtc)).ToList();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _documentRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            throw new InvalidOperationException("Document was not found.");
        }
    }

    public async Task<Document> UploadAsync(Guid chapterId, Guid userId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("File is empty.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException("File exceeds the 20MB limit.");
        }

        var chapterExists = await _chapterRepository.ExistsAsync(chapterId, cancellationToken);
        if (!chapterExists)
        {
            throw new InvalidOperationException("Invalid chapter.");
        }

        var text = await _extractor.ExtractAsync(file, cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Could not extract text content from the file.");
        }

        var document = new Document
        {
            Id = Guid.NewGuid(),
            ChapterId = chapterId,
            UploadedByUserId = userId,
            OriginalFileName = Path.GetFileName(file.FileName),
            FileType = Path.GetExtension(file.FileName).ToLowerInvariant(),
            FileSizeBytes = file.Length,
            ContentText = text,
            UploadedAtUtc = DateTime.UtcNow
        };

        await _documentRepository.AddAsync(document, cancellationToken);
        await _queue.QueueAsync(document.Id, cancellationToken);

        return document;
    }
}
