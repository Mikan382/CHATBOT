namespace PresentationLayer.ViewModels;

public class ChatIndexViewModel
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public bool FineTuneConfigured { get; set; }
    public bool GeminiConfigured { get; set; }
}
