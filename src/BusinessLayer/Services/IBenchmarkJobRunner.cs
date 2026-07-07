namespace BusinessLayer.Services;

public interface IBenchmarkJobRunner
{
    BenchmarkProgress GetProgress();
    bool TryStart(int questionLimit, out string? error);
}
