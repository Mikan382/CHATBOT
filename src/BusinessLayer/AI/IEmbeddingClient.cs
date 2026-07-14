namespace BusinessLayer.AI;

public interface IEmbeddingClient
{
    bool IsConfigured { get; }
    string ModelName { get; }
    IReadOnlyList<string> AvailableModels { get; }
    bool IsModelConfigured(string modelName);
    Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken);
    Task<float[]> EmbedPassageAsync(string text, CancellationToken cancellationToken);
    Task<float[]> EmbedQueryAsync(string modelName, string text, CancellationToken cancellationToken);
    Task<float[]> EmbedPassageAsync(string modelName, string text, CancellationToken cancellationToken);
}
