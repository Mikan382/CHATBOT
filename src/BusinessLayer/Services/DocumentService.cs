using BusinessLayer.DTOs;
using BusinessLayer.Indexing;
using BusinessLayer.Parsing;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Services;

public class DocumentService : IDocumentService
{
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

    public async Task<DocumentIndexPageDto> GetIndexDataAsync(
        string? searchTerm,
        Guid? courseId,
        Guid? chapterId,
        Guid userId,
        bool isAdmin,
        bool isTeacher,
        CancellationToken cancellationToken)
    {
        var teacherId = TeacherFilter(userId, isAdmin, isTeacher);
        var courses = await _courseRepository.ListAsync(null, teacherId, cancellationToken);
        var chapters = courses
            .SelectMany(x => x.Chapters)
            .OrderBy(x => x.Course?.Code)
            .ThenBy(x => x.Order)
            .ToList();
        var selectedCourseId = courseId.HasValue && courses.Any(x => x.Id == courseId.Value)
            ? courseId
            : null;
        var selectedChapter = chapterId.HasValue
            ? chapters.FirstOrDefault(x => x.Id == chapterId.Value)
            : null;
        Guid? selectedChapterId = selectedChapter is not null
            && (!selectedCourseId.HasValue || selectedChapter.CourseId == selectedCourseId.Value)
                ? selectedChapter.Id
                : null;
        var documents = await _documentRepository.ListWithChapterAndChunksAsync(
            searchTerm,
            selectedCourseId,
            selectedChapterId,
            teacherId,
            cancellationToken);

        var chapterDtos = chapters
            .Select(c => new ChapterSelectDto(c.Id, c.CourseId, c.Order, c.Title))
            .ToList();
        var documentDtos = documents.Select(ToIndexDto).ToList();
        var courseDtos = courses.Select(ToCourseDto).ToList();

        return new DocumentIndexPageDto(
            courseDtos,
            chapterDtos,
            documentDtos,
            selectedCourseId,
            selectedChapterId);
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
            string.IsNullOrWhiteSpace(doc.ChunkingStrategy) ? "unknown" : doc.ChunkingStrategy,
            doc.Chunks.OrderBy(c => c.ChunkIndex).Select(c => new DocumentChunkViewDto(
                c.ChunkIndex,
                c.Content,
                c.Embeddings.Select(e => e.ModelName).ToList()
            )).ToList());
    }

    public async Task DeleteAsync(Guid id, Guid userId, string userRole, CancellationToken cancellationToken)
    {
        var details = await _documentRepository.GetDetailsAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Document was not found.");

        await EnsureHeadTeacherAsync(details.Chapter!.CourseId, userId, userRole, cancellationToken);

        var deleted = await _documentRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            throw new InvalidOperationException("Document was not found.");
        }
    }

    public async Task<int> ReindexCourseAsync(Guid courseId, Guid userId, string userRole, CancellationToken cancellationToken)
    {
        await EnsureHeadTeacherAsync(courseId, userId, userRole, cancellationToken);

        var documents = await _documentRepository.ListWithChapterAndChunksAsync(
            searchTerm: null,
            courseId: courseId,
            chapterId: null,
            teacherId: null,
            cancellationToken);

        if (documents.Count == 0)
        {
            return 0;
        }

        var count = 0;
        foreach (var docSummary in documents)
        {
            var document = await _documentRepository.GetDetailsAsync(docSummary.Id, cancellationToken);
            if (document is null) continue;

            document.Chunks.Clear();
            await _indexingService.PopulateChunksAsync(document, cancellationToken);
            await _documentRepository.UpdateChunksAsync(document, cancellationToken);
            count++;
        }

        return count;
    }

    public async Task<Guid> UploadAsync(Guid chapterId, Guid userId, string userRole, Stream stream, string fileName, long fileSize, CancellationToken cancellationToken)
    {
        if (fileSize == 0)
        {
            throw new InvalidOperationException("File is empty.");
        }

        if (fileSize > DocumentUploadLimits.MaxFileSizeBytes)
        {
            throw new InvalidOperationException("File exceeds the 20MB limit.");
        }

        var chapter = await _chapterRepository.GetByIdAsync(chapterId, cancellationToken)
            ?? throw new InvalidOperationException("Invalid chapter.");

        await EnsureHeadTeacherAsync(chapter.CourseId, userId, userRole, cancellationToken);

        var text = await _extractor.ExtractAsync(stream, fileName, cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Could not extract text content from the file.");
        }

        var contentHash = DocumentContentHasher.Compute(text);
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

    private async Task EnsureHeadTeacherAsync(Guid courseId, Guid userId, string userRole, CancellationToken cancellationToken)
    {
        if (!await _courseRepository.TeacherIsHeadOfCourseAsync(courseId, userId, cancellationToken))
        {
            throw new InvalidOperationException("Only the assigned head teacher of this course can manage documents.");
        }
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
            doc.ChunksCount > 0 ? doc.ChunksCount : doc.Chunks.Count,
            string.IsNullOrWhiteSpace(doc.ChunkingStrategy) ? "unknown" : doc.ChunkingStrategy);
    }
}
