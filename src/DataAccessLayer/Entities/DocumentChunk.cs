namespace DataAccessLayer.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public ICollection<DocumentChunkEmbedding> Embeddings { get; set; } = new List<DocumentChunkEmbedding>();
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = "";
    public string SourceName { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
