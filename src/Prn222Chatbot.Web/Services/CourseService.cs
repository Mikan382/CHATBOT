using Prn222Chatbot.Web.Domain;
using Prn222Chatbot.Web.Repositories;

namespace Prn222Chatbot.Web.Services;

public class CourseService
{
    private readonly ICourseRepository _courseRepository;

    public CourseService(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
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

    private static ChapterDto ToDto(Chapter chapter)
    {
        return new ChapterDto(chapter.Id, chapter.Order, chapter.Clo, chapter.Title, chapter.Summary);
    }
}
