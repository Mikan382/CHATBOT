namespace BusinessLayer.Indexing;

public class BackgroundIndexingService : BackgroundService
{
    private readonly IIndexingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundIndexingService> _logger;

    public BackgroundIndexingService(IIndexingQueue queue, IServiceScopeFactory scopeFactory, ILogger<BackgroundIndexingService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Guid documentId;
            try
            {
                documentId = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var indexer = scope.ServiceProvider.GetRequiredService<DocumentIndexingService>();
                await indexer.IndexAsync(documentId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled indexing failure for document {DocumentId}", documentId);
            }
        }
    }
}
