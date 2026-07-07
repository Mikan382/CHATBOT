namespace BusinessLayer.Helpers;

public static class StringHelper
{
    /// <summary>
    /// Trims the value and throws if empty/whitespace.
    /// Replaces the 3 duplicated NormalizeRequired methods across CourseService, ChapterService, UserAdminService.
    /// </summary>
    public static string NormalizeRequired(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{label} is required.");
        }

        return value.Trim();
    }
}
