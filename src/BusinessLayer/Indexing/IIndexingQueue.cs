namespace BusinessLayer.Indexing;

public interface IIndexingQueue
{
    ValueTask QueueAsync(Guid documentId, CancellationToken cancellationToken = default);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
