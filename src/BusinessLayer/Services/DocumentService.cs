using System.Security.Cryptography;
using System.Text;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;
using BusinessLayer.Indexing;
using BusinessLayer.Parsing;
using BusinessLayer.Retrieval;

namespace BusinessLayer.Services;

public class DocumentService : IDocumentService
{
    private const long MaxFileSize = 20 * 1024 * 1024;
    private readonly IChapterRepository _chapterRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentTextExtractor _extractor;
    private readonly DocumentIndexingService _indexingService;

    public DocumentService(
        IChapterRepository chapterRepository,
        ICourseRepository courseRepository,
        IDocumentRepository documentRepository,
        IDocumentTextExtractor extractor,
        DocumentIndexingService indexingService)
    {
        _chapterRepository = chapterRepository;
        _courseRepository = courseRepository;
        _documentRepository = documentRepository;
        _extractor = extractor;
        _indexingService = indexingService;
    }

    public async Task<(IReadOnlyList<ChapterSelectDto> Chapters, IReadOnlyList<DocumentIndexDto> Documents)> GetIndexDataAsync(
        string? searchTerm,
        Guid? courseId,
        Guid? chapterId,
        Guid userId,
        bool isAdmin,
        bool isTeacher,
        CancellationToken cancellationToken)
    {
        var teacherId = TeacherFilter(userId, isAdmin, isTeacher);
        var courses = await _courseRepository.ListWithChaptersAsync(teacherId, cancellationToken);
        var courseIds = courses.Select(x => x.Id).ToHashSet();
        var chapters = courses
            .SelectMany(x => x.Chapters)
            .OrderBy(x => x.Course?.Code)
            .ThenBy(x => x.Order)
            .ToList();
        var documents = await _documentRepository.ListWithChapterAndChunksAsync(searchTerm, courseId, chapterId, teacherId, cancellationToken);

        var chapterDtos = chapters
            .Where(c => courseIds.Contains(c.CourseId))
            .Select(c => new ChapterSelectDto(c.Id, c.CourseId, c.Order, c.Title))
            .ToList();
        var documentDtos = documents.Select(ToIndexDto).ToList();

        return (chapterDtos, documentDtos);
    }

    public async Task<IReadOnlyList<CourseDto>> ListCoursesAsync(Guid userId, bool isAdmin, bool isTeacher, CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.ListWithChaptersAsync(TeacherFilter(userId, isAdmin, isTeacher), cancellationToken);
        return courses.Select(ToCourseDto).ToList();
    }

    public async Task<IReadOnlyList<DocumentApiDto>> ListDocumentsAsync(CancellationToken cancellationToken)
    {
        var documents = await _documentRepository.ListWithChapterAndChunksAsync(null, null, null, null, cancellationToken);
        return documents.Select(x => new DocumentApiDto(
            x.Id,
            x.OriginalFileName,
            x.FileType,
            x.FileSizeBytes,
            x.UploadedAtUtc,
            x.Chapter is null ? null : new ChapterDto(x.Chapter.Id, x.Chapter.Order, x.Chapter.Clo, x.Chapter.Title, x.Chapter.Summary),
            x.ChunksCount > 0 ? x.ChunksCount : x.Chunks.Count)).ToList();
    }

    public async Task<DocumentDetailsDto> GetDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var doc = await _documentRepository.GetDetailsAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Document was not found.");

        return new DocumentDetailsDto(
            doc.Id,
            doc.OriginalFileName,
            doc.FileType,
            doc.FileSizeBytes,
            doc.UploadedAtUtc,
            doc.Chapter?.Course?.Code,
            doc.Chapter?.Course?.Name,
            doc.Chapter?.Title,
            doc.UploadedByUser?.Email,
            doc.ContentText,
            doc.ContentHash,
            doc.Chunks.OrderBy(c => c.ChunkIndex).Select(c => new DocumentChunkViewDto(
                c.ChunkIndex,
                c.Content,
                c.Embeddings.Select(e => e.ModelName).ToList()
            )).ToList());
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

    public async Task DeleteAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var details = await _documentRepository.GetDetailsAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Document was not found.");

        if (!isAdmin && !await _courseRepository.TeacherCanManageCourseAsync(details.Chapter!.CourseId, userId, cancellationToken))
        {
            throw new InvalidOperationException("You are not assigned to this course.");
        }

        var deleted = await _documentRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            throw new InvalidOperationException("Document was not found.");
        }
    }

    public async Task<Guid> UploadAsync(Guid chapterId, Guid userId, bool isAdmin, Stream stream, string fileName, long fileSize, CancellationToken cancellationToken)
    {
        if (fileSize == 0)
        {
            throw new InvalidOperationException("File is empty.");
        }

        if (fileSize > MaxFileSize)
        {
            throw new InvalidOperationException("File exceeds the 20MB limit.");
        }

        var chapter = await _chapterRepository.GetByIdAsync(chapterId, cancellationToken)
            ?? throw new InvalidOperationException("Invalid chapter.");

        if (!isAdmin && !await _courseRepository.TeacherCanManageCourseAsync(chapter.CourseId, userId, cancellationToken))
        {
            throw new InvalidOperationException("You are not assigned to this course.");
        }

        var text = await _extractor.ExtractAsync(stream, fileName, cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Could not extract text content from the file.");
        }

        var contentHash = ComputeContentHash(text);
        if (await _documentRepository.ContentHashExistsAsync(chapterId, contentHash, cancellationToken))
        {
            throw new InvalidOperationException("This document content already exists in the selected chapter.");
        }

        var document = new Document
        {
            Id = Guid.NewGuid(),
            ChapterId = chapterId,
            UploadedByUserId = userId,
            OriginalFileName = Path.GetFileName(fileName),
            FileType = Path.GetExtension(fileName).ToLowerInvariant(),
            FileSizeBytes = fileSize,
            ContentText = text,
            ContentHash = contentHash,
            UploadedAtUtc = DateTime.UtcNow
        };

        await _indexingService.PopulateChunksAsync(document, cancellationToken);
        await _documentRepository.AddAsync(document, cancellationToken);

        return document.Id;
    }

    private static Guid? TeacherFilter(Guid userId, bool isAdmin, bool isTeacher)
    {
        return !isAdmin && isTeacher ? userId : null;
    }

    private static string ComputeContentHash(string text)
    {
        var normalized = TextNormalizer.Normalize(text);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }

    private static CourseDto ToCourseDto(Course course)
    {
        return new CourseDto(
            course.Id,
            course.Code,
            course.Name,
            course.Description,
            course.Tools,
            course.Chapters
                .OrderBy(c => c.Order)
                .Select(c => new ChapterDto(c.Id, c.Order, c.Clo, c.Title, c.Summary))
                .ToList());
    }

    private static DocumentIndexDto ToIndexDto(Document doc)
    {
        return new DocumentIndexDto(
            doc.Id,
            doc.OriginalFileName,
            doc.FileType,
            doc.FileSizeBytes,
            doc.UploadedAtUtc,
            doc.Chapter?.Course?.Code,
            doc.Chapter?.Title,
            doc.ChunksCount > 0 ? doc.ChunksCount : doc.Chunks.Count);
    }
}
