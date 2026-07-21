using BusinessLayer.DTOs;
using BusinessLayer.Helpers;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly IUserAdminRepository _userRepository;

    public CourseService(
        ICourseRepository courseRepository,
        IUserAdminRepository userRepository)
    {
        _courseRepository = courseRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<CourseDto>> ListDtosAsync(CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.ListAsync(null, null, cancellationToken);
        return courses.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<CourseDto>> ListManageDtosAsync(Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.ListAsync(null, TeacherFilter(userId, isAdmin), cancellationToken);
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
            course.TeacherAssignments.Select(x => (Guid?)x.TeacherUserId).FirstOrDefault(),
            course.DefaultChunkingStrategy,
            course.DefaultChunkSize,
            course.DefaultChunkOverlap,
            course.DefaultEmbeddingModel);
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
        Guid? teacherId,
        string? defaultChunkingStrategy = null,
        int? defaultChunkSize = null,
        int? defaultChunkOverlap = null,
        string? defaultEmbeddingModel = null,
        CancellationToken cancellationToken = default)
    {
        code = StringHelper.NormalizeRequired(code, "Course code");
        name = StringHelper.NormalizeRequired(name, "Course name");

        if (await _courseRepository.CodeExistsAsync(code, null, cancellationToken))
        {
            throw new InvalidOperationException("Course code already exists.");
        }

        var validTeacherId = await ValidateTeacherIdAsync(teacherId, cancellationToken);
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description?.Trim() ?? "",
            Tools = tools?.Trim() ?? "",
            DefaultChunkingStrategy = string.IsNullOrWhiteSpace(defaultChunkingStrategy) ? null : defaultChunkingStrategy.Trim(),
            DefaultChunkSize = defaultChunkSize > 0 ? defaultChunkSize : null,
            DefaultChunkOverlap = defaultChunkOverlap >= 0 ? defaultChunkOverlap : null,
            DefaultEmbeddingModel = string.IsNullOrWhiteSpace(defaultEmbeddingModel) ? null : defaultEmbeddingModel.Trim(),
            TeacherAssignments = validTeacherId is null
                ? new List<CourseTeacher>()
                : new List<CourseTeacher>
                {
                    new()
                    {
                        TeacherUserId = validTeacherId.Value,
                        IsHead = true,
                        AssignedAtUtc = DateTime.UtcNow
                    }
                }
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
        Guid? teacherId,
        string? defaultChunkingStrategy = null,
        int? defaultChunkSize = null,
        int? defaultChunkOverlap = null,
        string? defaultEmbeddingModel = null,
        CancellationToken cancellationToken = default)
    {
        var validTeacherId = await ValidateTeacherIdAsync(teacherId, cancellationToken);
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
        course.DefaultChunkingStrategy = string.IsNullOrWhiteSpace(defaultChunkingStrategy) ? null : defaultChunkingStrategy.Trim();
        course.DefaultChunkSize = defaultChunkSize > 0 ? defaultChunkSize : null;
        course.DefaultChunkOverlap = defaultChunkOverlap >= 0 ? defaultChunkOverlap : null;
        course.DefaultEmbeddingModel = string.IsNullOrWhiteSpace(defaultEmbeddingModel) ? null : defaultEmbeddingModel.Trim();
        await _courseRepository.SaveTeacherAssignmentAsync(course, validTeacherId, cancellationToken);
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

    private async Task<bool> CanManageCourseAsync(Guid courseId, Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        return isAdmin || await _courseRepository.TeacherCanManageCourseAsync(courseId, userId, cancellationToken);
    }

    private static Guid? TeacherFilter(Guid userId, bool isAdmin)
    {
        return isAdmin ? null : userId;
    }

    private async Task<Guid?> ValidateTeacherIdAsync(
        Guid? teacherId,
        CancellationToken cancellationToken)
    {
        if (teacherId is null)
        {
            return null;
        }

        var teachers = await _userRepository.ListUsersByRoleAsync(UserRoleNames.Teacher, cancellationToken);
        var activeTeacherIds = teachers.Select(x => x.Id).ToHashSet();
        if (!activeTeacherIds.Contains(teacherId.Value))
        {
            throw new InvalidOperationException("The selected user is not an active teacher.");
        }

        return teacherId;
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
                .ToList(),
            course.DefaultChunkingStrategy,
            course.DefaultChunkSize,
            course.DefaultChunkOverlap,
            course.DefaultEmbeddingModel);
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
                .Select(x => x.Teacher?.DisplayName ?? x.Teacher?.Email)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
            course.DefaultChunkingStrategy,
            course.DefaultChunkSize,
            course.DefaultChunkOverlap,
            course.DefaultEmbeddingModel);
    }
}
