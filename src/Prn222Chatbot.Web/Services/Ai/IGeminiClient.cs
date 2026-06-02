namespace Prn222Chatbot.Web.Services.Ai;

public interface IGeminiClient
{
    bool IsConfigured { get; }
    Task<string> GenerateAsync(string systemInstruction, string prompt, CancellationToken cancellationToken);
}
