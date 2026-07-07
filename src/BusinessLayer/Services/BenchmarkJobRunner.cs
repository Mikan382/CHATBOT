using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services;

public class BenchmarkProgress
{
    public int Total { get; set; }
    public int Done { get; set; }
    public bool Running { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Singleton that runs a full benchmark in a background Task.
/// Prevents concurrent runs and exposes live progress.
/// </summary>
public class BenchmarkJobRunner : IBenchmarkJobRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BenchmarkJobRunner> _logger;

    private readonly object _lock = new();
    private BenchmarkProgress _progress = new();
    private CancellationTokenSource? _cts;
    private Task? _runningTask;

    public BenchmarkJobRunner(IServiceScopeFactory scopeFactory, ILogger<BenchmarkJobRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public BenchmarkProgress GetProgress()
    {
        lock (_lock)
        {
            return new BenchmarkProgress
            {
                Total = _progress.Total,
                Done = _progress.Done,
                Running = _progress.Running,
                Error = _progress.Error
            };
        }
    }

    public bool TryStart(int questionLimit, out string? error)
    {
        lock (_lock)
        {
            if (_progress.Running)
            {
                error = "A benchmark is already running.";
                return false;
            }

            _progress = new BenchmarkProgress { Running = true, Total = 0, Done = 0 };
            _cts = new CancellationTokenSource();
        }

        error = null;
        var token = _cts.Token;
        _runningTask = Task.Run(() => RunAsync(questionLimit, token));
        return true;
    }

    public void Cancel()
    {
        lock (_lock)
        {
            _cts?.Cancel();
        }
    }

    private async Task RunAsync(int questionLimit, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var evalService = scope.ServiceProvider.GetRequiredService<IEvaluationService>();

            var strategies = evalService.AvailableChunkingStrategies;
            var models = evalService.AvailableEmbeddingModels;
            var total = questionLimit * strategies.Count * models.Count;

            lock (_lock) { _progress.Total = total; }

            await evalService.RunFullBenchmarkWithProgressAsync(
                questionLimit,
                () =>
                {
                    lock (_lock) { _progress.Done++; }
                },
                cancellationToken);

            lock (_lock) { _progress.Running = false; }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Background benchmark was cancelled");
            lock (_lock)
            {
                _progress.Running = false;
                _progress.Error = "Benchmark was cancelled.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background benchmark failed");
            lock (_lock)
            {
                _progress.Running = false;
                _progress.Error = ex.Message;
            }
        }
        finally
        {
            lock (_lock)
            {
                _cts?.Dispose();
                _cts = null;
            }
        }
    }
}
