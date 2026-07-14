namespace BusinessLayer.Helpers;

public static class StringHelper
{
    public static string NormalizeRequired(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{label} is required.");
        }

        return value.Trim();
    }
}
