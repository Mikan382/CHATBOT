namespace PresentationLayer.ViewModels;

using BusinessLayer.Services;

public class ChatIndexViewModel
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public Guid SelectedCourseId { get; set; }
    public IReadOnlyList<CourseDto> Courses { get; set; } = [];
    public bool FineTuneConfigured { get; set; }
    public bool GeminiConfigured { get; set; }
}
