using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using BusinessLayer.Helpers;

namespace BusinessLayer.Services;

public class ChapterService : IChapterService
{
    private readonly ICourseRepository _courseRepository;
    private readonly IChapterRepository _chapterRepository;

    public ChapterService(ICourseRepository courseRepository, IChapterRepository chapterRepository)
    {
        _courseRepository = courseRepository;
        _chapterRepository = chapterRepository;
    }

    public async Task<IReadOnlyList<ChapterDto>> ListByCourseAsync(Guid courseId, CancellationToken cancellationToken)
    {
        var chapters = await _chapterRepository.ListByCourseAsync(courseId, cancellationToken);
        return chapters.Select(ToDto).ToList();
    }

    public async Task<ChapterFormDto?> GetEditableAsync(Guid id, CancellationToken cancellationToken)
    {
        var chapter = await _chapterRepository.GetByIdAsync(id, cancellationToken);
        if (chapter is null) return null;
        return new ChapterFormDto(chapter.Id, chapter.CourseId, chapter.Order, chapter.Clo, chapter.Title, chapter.Summary);
    }

    public async Task<(Guid Id, Guid CourseId)> CreateAsync(Guid courseId, int order, string? clo, string title, string? summary, CancellationToken cancellationToken)
    {
        _ = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");

        title = StringHelper.NormalizeRequired(title, "Chapter title");
        ValidateOrder(order);

        if (await _chapterRepository.OrderExistsAsync(courseId, order, null, cancellationToken))
        {
            throw new InvalidOperationException("Chapter order already exists in this course.");
        }

        var chapter = new Chapter
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Order = order,
            Clo = clo?.Trim() ?? "",
            Title = title,
            Summary = summary?.Trim() ?? ""
        };

        await _chapterRepository.AddAsync(chapter, cancellationToken);
        return (chapter.Id, chapter.CourseId);
    }

    public async Task UpdateAsync(Guid id, Guid courseId, int order, string? clo, string title, string? summary, CancellationToken cancellationToken)
    {
        var chapter = await _chapterRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Chapter was not found.");

        _ = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");

        title = StringHelper.NormalizeRequired(title, "Chapter title");
        ValidateOrder(order);

        if (await _chapterRepository.OrderExistsAsync(courseId, order, id, cancellationToken))
        {
            throw new InvalidOperationException("Chapter order already exists in this course.");
        }

        chapter.CourseId = courseId;
        chapter.Order = order;
        chapter.Clo = clo?.Trim() ?? "";
        chapter.Title = title;
        chapter.Summary = summary?.Trim() ?? "";
        await _chapterRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        // Business rule moved from repository (fix #7)
        if (await _chapterRepository.HasDependenciesAsync(id, cancellationToken))
        {
            throw new InvalidOperationException("Cannot delete a chapter that still has documents or evaluation questions.");
        }

        var deleted = await _chapterRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            throw new InvalidOperationException("Chapter was not found.");
        }
    }

    private static ChapterDto ToDto(Chapter chapter)
    {
        return new ChapterDto(chapter.Id, chapter.Order, chapter.Clo, chapter.Title, chapter.Summary);
    }

    private static void ValidateOrder(int order)
    {
        if (order <= 0)
        {
            throw new InvalidOperationException("Chapter order must be greater than zero.");
        }
    }
}
