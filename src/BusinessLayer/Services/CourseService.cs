using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;
using BusinessLayer.Helpers;

namespace BusinessLayer.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly IChapterRepository _chapterRepository;
    private readonly IUserAdminRepository _userRepository;

    public CourseService(
        ICourseRepository courseRepository,
        IChapterRepository chapterRepository,
        IUserAdminRepository userRepository)
    {
        _courseRepository = courseRepository;
        _chapterRepository = chapterRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<CourseDto>> ListDtosAsync(CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.ListWithChaptersAsync(null, cancellationToken);
        return courses.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<CourseDto>> ListManageDtosAsync(Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.ListWithChaptersAsync(TeacherFilter(userId, isAdmin), cancellationToken);
        return courses.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<CourseListDto>> ListManageAsync(string? searchTerm, Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.ListAsync(searchTerm, TeacherFilter(userId, isAdmin), cancellationToken);
        return courses.Select(ToListDto).ToList();
    }

    public async Task<IReadOnlyList<TeacherOptionDto>> ListTeacherOptionsAsync(CancellationToken cancellationToken)
    {
        var teachers = await _userRepository.ListUsersByRoleAsync(UserRoleNames.Teacher, cancellationToken);
        return teachers
            .Select(x => new TeacherOptionDto(x.Id, x.Email, x.DisplayName))
            .ToList();
    }

    public async Task<CourseFormDto?> GetEditableAsync(Guid id, CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(id, cancellationToken);
        if (course is null) return null;
        return new CourseFormDto(
            course.Id,
            course.Code,
            course.Name,
            course.Description,
            course.Tools,
            course.TeacherAssignments.Select(x => x.TeacherUserId).ToList());
    }

    public async Task<CourseDto?> GetDetailsAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAsync(id, userId, isAdmin, cancellationToken))
        {
            return null;
        }

        var course = await _courseRepository.GetByIdAsync(id, cancellationToken);
        if (course is null) return null;
        return ToDto(course);
    }

    public async Task<Guid> CreateAsync(
        string code,
        string name,
        string? description,
        string? tools,
        IReadOnlyList<Guid> teacherIds,
        CancellationToken cancellationToken)
    {
        code = StringHelper.NormalizeRequired(code, "Course code");
        name = StringHelper.NormalizeRequired(name, "Course name");

        if (await _courseRepository.CodeExistsAsync(code, null, cancellationToken))
        {
            throw new InvalidOperationException("Course code already exists.");
        }

        var validTeacherIds = await ValidateTeacherIdsAsync(teacherIds, cancellationToken);
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description?.Trim() ?? "",
            Tools = tools?.Trim() ?? "",
            TeacherAssignments = validTeacherIds.Select(teacherId => new CourseTeacher
            {
                TeacherUserId = teacherId,
                AssignedAtUtc = DateTime.UtcNow
            }).ToList()
        };

        await _courseRepository.AddAsync(course, cancellationToken);
        return course.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        string code,
        string name,
        string? description,
        string? tools,
        IReadOnlyList<Guid> teacherIds,
        CancellationToken cancellationToken)
    {
        var validTeacherIds = await ValidateTeacherIdsAsync(teacherIds, cancellationToken);
        var course = await _courseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");

        code = StringHelper.NormalizeRequired(code, "Course code");
        name = StringHelper.NormalizeRequired(name, "Course name");

        if (await _courseRepository.CodeExistsAsync(code, id, cancellationToken))
        {
            throw new InvalidOperationException("Course code already exists.");
        }

        course.Code = code;
        course.Name = name;
        course.Description = description?.Trim() ?? "";
        course.Tools = tools?.Trim() ?? "";
        await _courseRepository.SaveWithTeacherAssignmentsAsync(course, validTeacherIds, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await _courseRepository.HasChaptersAsync(id, cancellationToken))
        {
            throw new InvalidOperationException("Cannot delete a course that still has chapters.");
        }

        var deleted = await _courseRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            throw new InvalidOperationException("Course was not found.");
        }
    }

    public async Task<IReadOnlyList<ChapterDto>> ListChaptersAsync(Guid courseId, Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAsync(courseId, userId, isAdmin, cancellationToken))
        {
            throw new InvalidOperationException("You are not assigned to this course.");
        }

        var chapters = await _chapterRepository.ListByCourseAsync(courseId, cancellationToken);
        return chapters.Select(ToDto).ToList();
    }

    private async Task<bool> CanManageCourseAsync(Guid courseId, Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        return isAdmin || await _courseRepository.TeacherCanManageCourseAsync(courseId, userId, cancellationToken);
    }

    private static Guid? TeacherFilter(Guid userId, bool isAdmin)
    {
        return isAdmin ? null : userId;
    }

    private async Task<IReadOnlyList<Guid>> ValidateTeacherIdsAsync(
        IReadOnlyList<Guid> teacherIds,
        CancellationToken cancellationToken)
    {
        var selectedIds = teacherIds.Distinct().ToList();
        if (selectedIds.Count == 0)
        {
            return selectedIds;
        }

        var activeTeacherIds = (await _userRepository.ListUsersByRoleAsync(UserRoleNames.Teacher, cancellationToken))
            .Select(x => x.Id)
            .ToHashSet();
        if (selectedIds.Any(x => !activeTeacherIds.Contains(x)))
        {
            throw new InvalidOperationException("One or more selected users are not active teachers.");
        }

        return selectedIds;
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

    private static CourseListDto ToListDto(Course course)
    {
        return new CourseListDto(
            course.Id,
            course.Code,
            course.Name,
            course.Description,
            course.Tools,
            course.Chapters.Count,
            course.TeacherAssignments
                .Select(x => x.Teacher?.DisplayName ?? x.Teacher?.Email ?? "")
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .OrderBy(x => x)
                .ToList());
    }
}
