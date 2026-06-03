namespace BusinessLayer.AI;

public interface IEmbeddingClient
{
    bool IsConfigured { get; }
    string ModelName { get; }
    Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken);
    Task<float[]> EmbedPassageAsync(string text, CancellationToken cancellationToken);
}
