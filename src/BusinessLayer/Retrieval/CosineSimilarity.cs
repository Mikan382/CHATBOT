namespace BusinessLayer.Retrieval;

/// <summary>
/// Shared cosine similarity helper for embedding retrieval.
/// </summary>
public static class CosineSimilarity
{
    public static double Cosine(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
        if (left.Length == 0 || left.Length != right.Length)
        {
            return 0;
        }

        var dimensions = left.Length;

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
