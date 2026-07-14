using BusinessLayer.DTOs;

namespace PresentationLayer.ViewModels;

public class ChatIndexViewModel
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public Guid SelectedCourseId { get; set; }
    public IReadOnlyList<CourseDto> Courses { get; set; } = [];
    public bool GeminiConfigured { get; set; }
    public ChatQuotaStatusDto? Quota { get; set; }
}
