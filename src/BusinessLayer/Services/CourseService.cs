using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Services;

public class CourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly IChapterRepository _chapterRepository;

    public CourseService(ICourseRepository courseRepository, IChapterRepository chapterRepository)
    {
        _courseRepository = courseRepository;
        _chapterRepository = chapterRepository;
    }

    public async Task<CourseDto> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetCurrentWithChaptersAsync(cancellationToken);
        return new CourseDto(
            course.Id,
            course.Code,
            course.Name,
            course.Description,
            course.Tools,
            course.Chapters
                .OrderBy(x => x.Order)
                .Select(ToDto)
                .ToList());
    }

    public async Task<IReadOnlyList<CourseDto>> ListDtosAsync(CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.ListWithChaptersAsync(cancellationToken);
        return courses.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<CourseListDto>> ListAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.ListAsync(searchTerm, cancellationToken);
        return courses.Select(x => new CourseListDto(
            x.Id,
            x.Code,
            x.Name,
            x.Description,
            x.Tools,
            x.Chapters.Count)).ToList();
    }

    public async Task<Course?> GetEditableAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _courseRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<CourseDto> GetDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");
        return ToDto(course);
    }

    public async Task<Course> CreateAsync(string code, string name, string? description, string? tools, CancellationToken cancellationToken)
    {
        code = NormalizeRequired(code, "Course code");
        name = NormalizeRequired(name, "Course name");

        if (await _courseRepository.CodeExistsAsync(code, null, cancellationToken))
        {
            throw new InvalidOperationException("Course code already exists.");
        }

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description?.Trim() ?? "",
            Tools = tools?.Trim() ?? ""
        };

        await _courseRepository.AddAsync(course, cancellationToken);
        return course;
    }

    public async Task UpdateAsync(Guid id, string code, string name, string? description, string? tools, CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");

        code = NormalizeRequired(code, "Course code");
        name = NormalizeRequired(name, "Course name");

        if (await _courseRepository.CodeExistsAsync(code, id, cancellationToken))
        {
            throw new InvalidOperationException("Course code already exists.");
        }

        course.Code = code;
        course.Name = name;
        course.Description = description?.Trim() ?? "";
        course.Tools = tools?.Trim() ?? "";
        await _courseRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _courseRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            throw new InvalidOperationException("Course was not found.");
        }
    }

    public async Task<IReadOnlyList<ChapterDto>> ListChaptersAsync(Guid courseId, CancellationToken cancellationToken)
    {
        var chapters = await _chapterRepository.ListByCourseAsync(courseId, cancellationToken);
        return chapters.Select(ToDto).ToList();
    }

    private static ChapterDto ToDto(Chapter chapter)
    {
        return new ChapterDto(chapter.Id, chapter.Order, chapter.Clo, chapter.Title, chapter.Summary);
    }

    private static CourseDto ToDto(Course course)
    {
        return new CourseDto(
            course.Id,
            course.Code,
            course.Name,
            course.Description,
            course.Tools,
            course.Chapters
                .OrderBy(x => x.Order)
                .Select(ToDto)
                .ToList());
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
