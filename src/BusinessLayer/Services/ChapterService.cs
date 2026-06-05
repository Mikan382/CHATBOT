using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Services;

public class ChapterService
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

    public async Task<Chapter?> GetEditableAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _chapterRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Chapter> CreateAsync(Guid courseId, int order, string? clo, string title, string? summary, CancellationToken cancellationToken)
    {
        _ = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");

        title = NormalizeRequired(title, "Chapter title");
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
        return chapter;
    }

    public async Task UpdateAsync(Guid id, Guid courseId, int order, string? clo, string title, string? summary, CancellationToken cancellationToken)
    {
        var chapter = await _chapterRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Chapter was not found.");

        _ = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");

        title = NormalizeRequired(title, "Chapter title");
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

    private static string NormalizeRequired(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{label} is required.");
        }

        return value.Trim();
    }
}
