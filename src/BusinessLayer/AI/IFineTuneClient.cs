namespace BusinessLayer.AI;

public interface IFineTuneClient
{
    bool IsConfigured { get; }
    Task<FineTuneResponse> GenerateAsync(FineTuneRequest request, CancellationToken cancellationToken);
}
