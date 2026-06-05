using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid ChapterId { get; set; }
    public Chapter? Chapter { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public ApplicationUser? UploadedByUser { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string FileType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public string ContentText { get; set; } = "";
    public DocumentIndexStatus IndexStatus { get; set; } = DocumentIndexStatus.Pending;
    public int IndexProgressPercent { get; set; }
    public string IndexStage { get; set; } = "Queued";
    public string? IndexError { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
