namespace BusinessLayer.Retrieval;

/// <summary>
/// Shared cosine similarity calculation.
/// Replaces the 2 duplicated private Cosine methods in RetrievalService and BenchmarkRetrievalService.
/// </summary>
public static class CosineSimilarity
{
    public static double Cosine(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
        var dimensions = Math.Min(left.Length, right.Length);
        if (dimensions == 0)
        {
            return 0;
        }

        var dot = 0d;
        var leftMagnitude = 0d;
        var rightMagnitude = 0d;
        for (var i = 0; i < dimensions; i++)
        {
            dot += left[i] * right[i];
            leftMagnitude += left[i] * left[i];
            rightMagnitude += right[i] * right[i];
        }

        if (leftMagnitude <= 0 || rightMagnitude <= 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }
}
