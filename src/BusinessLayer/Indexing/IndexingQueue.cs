using System.Threading.Channels;

namespace BusinessLayer.Indexing;

public class IndexingQueue : IIndexingQueue
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

    public ValueTask QueueAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return _queue.Writer.WriteAsync(documentId, cancellationToken);
    }

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAsync(cancellationToken);
    }
}
