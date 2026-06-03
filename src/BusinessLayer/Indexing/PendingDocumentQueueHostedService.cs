using DataAccessLayer.Repositories;

namespace BusinessLayer.Indexing;

public class PendingDocumentQueueHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IIndexingQueue _queue;

    public PendingDocumentQueueHostedService(IServiceScopeFactory scopeFactory, IIndexingQueue queue)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var pendingIds = await documentRepository.ListPendingOrProcessingIdsAsync(cancellationToken);

        foreach (var documentId in pendingIds)
        {
            await _queue.QueueAsync(documentId, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
