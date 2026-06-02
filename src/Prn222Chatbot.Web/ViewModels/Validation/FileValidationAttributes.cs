using System.ComponentModel.DataAnnotations;

namespace Prn222Chatbot.Web.ViewModels.Validation;

public class AllowedFileExtensionsAttribute : ValidationAttribute
{
    private readonly HashSet<string> _extensions;

    public AllowedFileExtensionsAttribute(params string[] extensions)
    {
        _extensions = extensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not IFormFile file)
        {
            return new ValidationResult("Invalid upload payload.");
        }

        var extension = Path.GetExtension(file.FileName);
        return _extensions.Contains(extension)
            ? ValidationResult.Success
            : new ValidationResult($"Only {string.Join(", ", _extensions)} files are supported.");
    }
}

public class MaxFileSizeAttribute : ValidationAttribute
{
    private readonly long _maxBytes;

    public MaxFileSizeAttribute(long maxBytes)
    {
        _maxBytes = maxBytes;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not IFormFile file)
        {
            return new ValidationResult("Invalid upload payload.");
        }

        return file.Length <= _maxBytes
            ? ValidationResult.Success
            : new ValidationResult($"File size must be {FormatBytes(_maxBytes)} or smaller.");
    }

    private static string FormatBytes(long bytes)
    {
        return bytes >= 1024 * 1024
            ? $"{bytes / (1024 * 1024)}MB"
            : $"{bytes / 1024}KB";
    }
}
