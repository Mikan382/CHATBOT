namespace BusinessLayer.Services;

public static class DocumentUploadLimits
{
    public const long MaxFileSizeBytes = 20 * 1024 * 1024;
    public const long MaxRequestBodyBytes = MaxFileSizeBytes + (1024 * 1024);
    public const int MaxChunksPerDocument = 100;
}
