namespace DataAccessLayer.Entities;

public class DocumentChunkEmbedding
{
    public Guid Id { get; set; }
    public Guid DocumentChunkId { get; set; }
    public DocumentChunk? DocumentChunk { get; set; }
    public string ModelName { get; set; } = "";
    public int Dimensions { get; set; }
    public string VectorJson { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
