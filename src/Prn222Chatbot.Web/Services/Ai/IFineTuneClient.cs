namespace Prn222Chatbot.Web.Services.Ai;

public interface IFineTuneClient
{
    bool IsConfigured { get; }
    Task<FineTuneResponse> GenerateAsync(FineTuneRequest request, CancellationToken cancellationToken);
}
