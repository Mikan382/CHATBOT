namespace DataAccessLayer.Entities;

public class StudentUploadedDocument
{
    public Guid Id { get; set; }
    public Guid ChatSessionId { get; set; }
    public ChatSession? ChatSession { get; set; }
    public Guid UploadedByUserId { get; set; }
    public ApplicationUser? UploadedByUser { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string FileType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public string ContentText { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public string ChunkingStrategy { get; set; } = "";
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
