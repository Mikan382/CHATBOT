using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BusinessLayer.Indexing;

public static class DocumentContentHasher
{
    public static string Compute(string text)
    {
        var normalized = Regex.Replace(
            text.Normalize(NormalizationForm.FormC),
            @"\s+",
            " ").Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }
}
